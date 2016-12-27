using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Emitters.NASM
{
	public class NasmEmitter : BCMBase<NasmType, NasmFunction, NasmHolder, NasmEmitterScope>
	{
		public const string PrintSFormatName = "prints";
		public const string PrintIFormatName = "printi";
		public const string StringConstName = "stringconst";
		public const string PrintSFunctionLabel = "_cprintS";
		public const string PrintIFunctionLabel = "_cprintI";
		public const string MallocLabel = "_malloc";
		public const string FreeLabel = "_free";
		public const int Null = 0;

		public const int ErrorCode = 0xe7707;

		[InArgument(Comment = "the name of the output file, empty for standar output", ConsoleShortName = "-o", DefaultValue ="out.asm")]
		public string OutputFile { get; set; }

		readonly FormatWriter fw;

		TextWriter t;
		FileStream toclose;

		readonly Dictionary<string, NasmFunction> std;
		readonly HashSet<string> Externs;
		Dictionary<string, NasmStringConst> StringConst;
		int StringConstEnd;
		readonly NasmFunction PrintS;

		public NasmEmitter()
		{
			fw = new FormatWriter();
			Externs = new HashSet<string>();
			std = new Dictionary<string, NasmFunction>();

			PrintS = new NasmCFunction(PrintSFunctionLabel, false, this, true, "PrintS");
			std["prints"] = PrintS;

			NasmType.Int = new NasmType(NasmRefType.None);
			NasmType.String = new NasmType(NasmRefType.Dynamic, -1);
			NasmType.DWordRMemberAccess = new NasmMacroFunction(MemberReadAccess, this, "MemberReadAccess");
			NasmType.DWordWMemberAccess = new NasmMacroFunction(MemberWriteAccess, this,"MemberWriteAccess");
			NasmType.ByteRMemberAccess = new NasmMacroFunction(MemberReadAccessByteString, this, "MemberReadAccess(Bytes)");
			NasmType.ArrayAllocator = new NasmMacroFunction(ArrayAllocator, this, "ArrayAllocator") { Requested = new[] { Register.ECX, Register.EAX } };
			NasmType.ByteZeroEndArrayAllocator = new NasmMacroFunction(ArrayAllocatorBytesZeroEnd, this, "ArrayAllocator") { Requested = new[] { Register.ECX, Register.EAX } };
		}

		public override void InitializeCodeGeneration(ErrorReport report)
		{
			base.InitializeCodeGeneration(report);

			if (string.IsNullOrWhiteSpace(OutputFile))
				t = Console.Out;
			else
			{
				toclose = new FileStream(OutputFile, FileMode.Create, FileAccess.Write);
				t = new StreamWriter(toclose);

			}

			StringConst = new Dictionary<string, NasmStringConst>();
			StringConstEnd = 0;
			AddConstant("Null Reference");

			fw.WriteLine("%include \"io.inc\"");
			fw.WriteLine("section .data");
			fw.WriteLine($"{PrintSFormatName} db '%', 's', 0");
			fw.WriteLine($"{PrintIFormatName} db '%', 'i', 0");
			fw.WriteLine(string.Format("{1}{0}{2}", fw.IndexOfFormat, '{', '}'), (Func<string>)(() =>
			{
				if (StringConst.Count == 0) return ";no string const\n";
				StringBuilder sb = new StringBuilder();
				sb.Append($"{StringConstName} db ");

				var dict = StringConst.ToList();
				dict.Sort((a, b) => a.Value.offset.CompareTo(b.Value.offset));
                foreach (var item in dict)
				{
					int size = item.Key.Length;
					for (int i = 0; i < 4; i++)
					{
						sb.Append((byte)size);
						sb.Append(',');
						size >>= 8;
					}
					foreach (var s in item.Key)
					{
						sb.Append((byte)s);
						sb.Append(',');
					}
					sb.Append((byte)0);
					sb.Append(',');
				}
				sb.Append((byte)0);
				return sb.ToString();
			}));

			fw.WriteLine("section .text");
			fw.WriteLine(";externs");
			fw.WriteLine(string.Format("{1}{0}{2}", fw.IndexOfFormat, '{', '}'), (Func<string>)(() =>
			{
				AddExterns();
				StringBuilder sb = new StringBuilder();
				foreach (var ex in Externs)
					sb.Append($"extern {ex}\n");
				return sb.ToString();
			}));
			NasmFunction.Malloc = new NasmCFunction(MallocLabel, true, this,name: "Malloc");
			NasmFunction.Free = new NasmCFunction(FreeLabel, true, this, name: "Free");

			fw.WriteLine("global CMAIN");
			BlankLine();
			fw.WriteLine(string.Format("{1}{0}{2}", fw.IndexOfFormat, '{', '}'), (Func<string>)(STD));
		}
		public override void End()
		{
			fw.Flush(t);
			if (toclose != null)
			{
				t.Close();
				toclose.Close();
				toclose.Dispose();
				toclose = null;
				t = null;
			}
		}

		#region [Control]
		public override void Comment(string comment)
		{
			fw.WriteLine(comment.Replace("\n\r", "\n").Replace("\n", "\n;"));
		}
		public override void BlankLine()
		{
			fw.WriteLine("");
		}

		public override void EmitError(int code, string message = null)
		{
			SetLabel();
			EmitError(fw, CurrentScope, this, code, message);
		}

		/// <summary>
		/// invalidate the holder making it eligible for temp holders and optimizations
		/// [IMPLEMENTATION_TIP] Should not be used with the same meaning this point on.
		/// [IMPLEMENTATION_TIP] All off-scope holders become invalid, there is no point in doing this at the end of a scope
		/// </summary>
		/// <param name="holder"></param>
		public override void Release(NasmHolder holder)
		{
			holder.DeclaratingScope.ReleasedTempVars.Enqueue(holder.DeclaringScopeIndex);
		}
		#endregion

		#region [Bind]

		#region [Holders]
		public override NasmHolder AddConstant(int value)
		{
			return new NasmIntConst(this, value);
		}
		public override NasmHolder AddConstant(string value)
		{
			NasmStringConst svar;
			if (StringConst.TryGetValue(value, out svar)) return svar;

			svar = new NasmStringConst(this, value, StringConstName, StringConstEnd);
			StringConst[value] = svar;
			StringConstEnd += value.Length + 5;// 4 for size and 1 for \0
			return svar;
		}

		public override NasmHolder BindVar(NasmType type = null, NasmHolder defaultvalue = null, string name = null,
		                                   bool global = false)
		{
			NasmHolder v;
			if (name != null || CurrentScope.ReleasedTempVars.Count <= 0)
			{
				fw.WriteLine($"; {name}<EBP - {(CurrentScope.VarsCount + 1) * 4}>");
				v = new NasmHolder(this, CurrentScope, CurrentScope.VarsCount);
				CurrentScope.VarsCount++;
			}
			else
				v = new NasmHolder(this, CurrentScope, CurrentScope.ReleasedTempVars.Dequeue());

			if (defaultvalue == null)
				defaultvalue = AddConstant(0);

			InstrAssing(v, defaultvalue);

			return v;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override NasmHolder StaticMemberAcces(NasmType tigertype, NasmHolder op1, int index)
		{
			if (index < 0)
				Report.Add(new StaticError(SourceLine, SourceColumn, "Static reference index must be non-negative",
				                                ErrorLevel.Error));
			bool dynamicupperboundcheck = false;
			switch (tigertype.RefType)
			{
				case NasmRefType.None:
					Report.Add(new StaticError(SourceLine, SourceColumn, "The provided type has no members to be referenced",
					                                ErrorLevel.Internal));
					break;
				case NasmRefType.Fixed:
					if (index >= tigertype.AsRefSize)
						Report.Add(new StaticError(SourceLine, SourceColumn, "Static reference index exceeds members count",
						                                ErrorLevel.Internal));
					break;
				//case NasmRefType.Dynamic:
				//case NasmRefType.NoSet:
				default:
					dynamicupperboundcheck = true;
					break;
			}
			return new NasmReference(op1, index, this, CurrentScope,
			                         ReferenceEquals(tigertype, NasmType.String)? WordSize.Byte : WordSize.DWord,
			                         dynamicupperboundcheck);
		}
		#endregion

		#region [Functions]
		[ScopeChanger(Reason = "Creates and enters in the primary scope of the program, this has no parent scope after closing it no fouther instructions can be emitted", ScopeName = "Main")]
		public override NasmFunction EntryPoint(bool returns = false, bool stringparams = false)
		{
			CurrentScope = new NasmEmitterScope(null, g.GNext(), g.GNext(), g.GNext(), g.GNext(), NasmScopeType.CFunction, 2);

			fw.WriteLine(";Main");
			fw.WriteLine("CMAIN:");
			fw.WriteLine($"_{CurrentScope.BeforeEnterScope:N}:");
			fw.IncrementIndentation();
			CurrentScope.WriteEnteringCode(fw);
			fw.WriteLine($"_{CurrentScope.BiginScope:N}:");

			return new NasmCFunction(CurrentScope.BiginScope.ToString(), false, this, name: "Main") { Bounded = true };
		}

		public override NasmFunction DeclareFunction(string name, NasmType returntype, Tuple<string, NasmType>[] args, bool global = false)
		{
			var func = new NasmFunction(CurrentScope, CurrentScope.VarsCount, this) { ParamsCount = args.Length };
			CurrentScope.FuncTypePos.Add(func);
			CurrentScope.VarsCount++;
			return func;
		}

		[ScopeChanger(Reason = "Creates and enters in a function scope", ScopeName = "Funcion_<name>")]
		public override NasmFunction BindFunction(string name, NasmType returntype, Tuple<string, NasmType>[] args, bool global = false)
		{
			var func = DeclareFunction(name, returntype, args, global);
			BindFunction(func);

			return func;
		}
		[ScopeChanger(Reason = "Creates and enters in a function scope", ScopeName = "AheadedFuncion_<name>")]
		public override void BindFunction(NasmFunction aheadedfunction)
		{
			CurrentScope = new NasmEmitterScope(CurrentScope, g.GNext(), g.GNext(), g.GNext(), g.GNext(), NasmScopeType.TigerFunction, aheadedfunction.ParamsCount, aheadedfunction);

			fw.WriteLine(";" + aheadedfunction.Name);
			fw.WriteLine($"jmp _{CurrentScope.AfterEndScope:N}");
			fw.WriteLine($"_{CurrentScope.BeforeEnterScope:N}:");
			fw.IncrementIndentation();
			CurrentScope.WriteEnteringCode(fw);
			fw.WriteLine($"_{CurrentScope.BiginScope:N}:");
		}
		#endregion

		#region [Types]
		public override NasmType DeclareType(string name)
		{
			var type = new NasmType(NasmRefType.NoSet, name: name);
			return type;
		}

		public override NasmType BindRecordType(string name, Tuple<string, NasmType>[] members, bool global = false)
		{
			var type = DeclareType(name);
			BindRecordType(type, members, global);
			return type;
		}
		public override void BindRecordType(NasmType aheadedtype, Tuple<string, NasmType>[] members, bool global = false)
		{
			aheadedtype.RefType = NasmRefType.Fixed;
			aheadedtype.AsRefSize = members.Length;
			SetLabel();

			CurrentScope = new NasmEmitterScope(CurrentScope, g.GNext(), g.GNext(), g.GNext(), g.GNext(), NasmScopeType.CFunction, members.Length);
			fw.WriteLine(";record " + aheadedtype.Name);
			fw.WriteLine($"jmp _{CurrentScope.AfterEndScope:N}");
			fw.WriteLine($"_{CurrentScope.BeforeEnterScope:N}:");
			fw.IncrementIndentation();

			fw.WriteLine($"mov {Register.ECX}, {(members.Length + 1) * 4}");
			fw.WriteLine("push " + Register.ECX);
			fw.WriteLine("call " + MallocLabel);
			fw.WriteLine($"add {Register.ESP}, 4 ");

			fw.WriteLine($"mov {Register.ECX}, {members.Length}");
			fw.WriteLine($"mov [{Register.EAX}], {Register.ECX}");
			fw.WriteLine($"mov {Register.EDI}, {Register.EAX}");
			fw.WriteLine($"add {Register.EDI}, {4}");

			fw.WriteLine($"mov {Register.ESI}, {Register.ESP}");
			fw.WriteLine($"add {Register.ESI}, {4}");

			fw.WriteLine("cdl");
			fw.WriteLine("rep movsd");

			fw.WriteLine("ret");
			fw.DecrementIndentation();
			fw.WriteLine($"_{CurrentScope.AfterEndScope:N}:");

			var label = CurrentScope.BeforeEnterScope;
			CurrentScope = CurrentScope.Parent;

			var f = new NasmFunction(CurrentScope, CurrentScope.VarsCount, this) { ParamsCount = members.Length, CFunction = true };
			CurrentScope.FuncTypePos.Add(f);
			CurrentScope.VarsCount++;
			var reg = CurrentScope.Lock.LockGPR(Register.EAX);
			bool stackback = reg == null;
			if (stackback)
			{
				fw.WriteLine("push " + Register.EAX);
				reg = Register.EAX;
			}

			NasmFunction.AlocateFunction(fw, reg.Value, CurrentScope, this, label);
			f.StackBackValue(reg.Value, fw, CurrentScope);

			if (stackback) fw.WriteLine("pop " + Register.EAX);
			else CurrentScope.Lock.Release(reg.Value);

			aheadedtype.Allocator = f;
		}

		public override NasmType BindArrayType(string name, NasmType underlayingtype)
		{
			var type = DeclareType(name);
			BindArrayType(type, underlayingtype);
			return type;
		}
		public override void BindArrayType(NasmType aheadedtype, NasmType underlayingtype)
		{
			aheadedtype.RefType = NasmRefType.Dynamic;
			aheadedtype.AsRefSize = -1;
		}
		#endregion

		#region [STD]

		public override bool TryBindSTDType(string name, out NasmType type)
		{
			switch (name.ToLower())
			{
				case "int":
					type = NasmType.Int;
					return true;

				case "string":
					type = NasmType.String;
					return true;

				default:
					type = null;
					return false;
			}
		}

		public override bool TryBindSTDFunction(string name, out NasmFunction function)
		{
			if (std.TryGetValue(name.ToLower(), out function)) return true;
			switch (name.ToLower())
			{
				case "prints":
					function = std["prints"];
					return true;

				case "printi":
					function = std["printi"] = new NasmCFunction(PrintIFunctionLabel, false, this, name: "PrintI");
					return true;

				default:
					function = null;
					return false;
			}
		}

		public override bool TryBindSTDConst(string name, out NasmHolder constant)
		{
			constant = null;
			if (name != "nil") return false;

			constant = new NasmIntConst(this, Null);
			return true;
		}

		#endregion

		#endregion

		#region [General Instructions]

		/// <summary>
		/// </summary>
		/// <param name="dest_nonconst"></param>
		/// <param name="value"></param>
		public override void InstrAssing(NasmHolder dest_nonconst, NasmHolder value)
		{
			SetLabel();
			var reg = CurrentScope.Lock.LockGPR(Register.EAX);
			bool stackback = reg == null;
			if (stackback)
			{
				fw.WriteLine("push " + Register.EAX);
				reg = Register.EAX;
			}

			value.PutValueInRegister(reg.Value, fw, CurrentScope);
			dest_nonconst.StackBackValue(reg.Value, fw, CurrentScope);

			if (stackback) fw.WriteLine("pop " + Register.EAX);
			else CurrentScope.Lock.Release(reg.Value);
		}

		public override void InstrAdd(NasmHolder dest_nonconst, NasmHolder op1, NasmHolder op2)
		{
			SetLabel();
			var reg = CurrentScope.Lock.LockGPR(Register.EAX);
			bool stackback = reg == null;
			if (stackback)
			{
				fw.WriteLine("push " + Register.EAX);
				reg = Register.EAX;
			}

			if (op1 is NasmIntConst && op2 is NasmIntConst)
			{
				fw.Write($"mov {reg}, {((NasmIntConst)op1).value + ((NasmIntConst)op2).value}");
			}
			else if (op1 is NasmIntConst)
			{
				op2.PutValueInRegister(reg.Value, fw, CurrentScope);
				fw.Write($"add {reg}, {((NasmIntConst)op1).value}");
			}
			else if (op2 is NasmIntConst)
			{
				op1.PutValueInRegister(reg.Value, fw, CurrentScope);
				fw.Write($"add {reg}, {((NasmIntConst)op2).value}");
			}
			else
			{
				var regB = CurrentScope.Lock.LockGPR(Register.EDX);
				bool stackbackB = regB == null;
				if (stackbackB)
				{
					regB = reg == Register.EDX? Register.EBX : Register.EDX;
					fw.WriteLine("push " + regB);
				}

				op1.PutValueInRegister(reg.Value, fw, CurrentScope);
				op2.PutValueInRegister(regB.Value, fw, CurrentScope);

				fw.Write($"add {reg}, {regB}");

				if (stackbackB) fw.WriteLine("pop " + regB);
				else CurrentScope.Lock.Release(regB.Value);
			}

			dest_nonconst.StackBackValue(reg.Value, fw, CurrentScope);
			if (stackback) fw.WriteLine("pop " + Register.EAX);
			else CurrentScope.Lock.Release(reg.Value);
		}

		public override void InstrSub(NasmHolder dest_nonconst, NasmHolder op1, NasmHolder op2)
		{
			SetLabel();
			var reg = CurrentScope.Lock.LockGPR(Register.EAX);
			bool stackback = reg == null;
			if (stackback)
			{
				fw.WriteLine("push " + Register.EAX);
				reg = Register.EAX;
			}

			if (op1 is NasmIntConst && op2 is NasmIntConst)
			{
				fw.Write($"mov {reg}, {((NasmIntConst)op1).value - ((NasmIntConst)op2).value}");
			}
			else if (op1 is NasmIntConst)
			{
				op2.PutValueInRegister(reg.Value, fw, CurrentScope);
				fw.Write($"sub {reg}, {((NasmIntConst)op1).value}");
			}
			else if (op2 is NasmIntConst)
			{
				op1.PutValueInRegister(reg.Value, fw, CurrentScope);
				fw.Write($"sub {reg}, {((NasmIntConst)op2).value}");
			}
			else
			{
				var regB = CurrentScope.Lock.LockGPR(Register.EDX);
				bool stackbackB = regB == null;
				if (stackbackB)
				{
					regB = reg == Register.EDX ? Register.EBX : Register.EDX;
					fw.WriteLine("push " + regB);

				}

				op1.PutValueInRegister(reg.Value, fw, CurrentScope);
				op2.PutValueInRegister(regB.Value, fw, CurrentScope);

				fw.Write($"sub {reg}, {regB}");

				if (stackbackB) fw.WriteLine("pop " + regB);
				else CurrentScope.Lock.Release(regB.Value);
			}

			dest_nonconst.StackBackValue(reg.Value, fw, CurrentScope);
			if (stackback) fw.WriteLine("pop " + Register.EAX);
			else CurrentScope.Lock.Release(reg.Value);
		}

		public override void InstrMult(NasmHolder dest_nonconst, NasmHolder op1, NasmHolder op2)
		{
			SetLabel();
			bool stackback;

			if (op1 is NasmIntConst && op2 is NasmIntConst)
			{
				var reg = CurrentScope.Lock.LockGPR(Register.EAX);
				stackback = reg == null;
				if (stackback)
				{
					reg = Register.EAX;
					fw.WriteLine("push " + Register.EAX);
				}

				fw.WriteLine($"mov {reg}, {((NasmIntConst)op1).value * ((NasmIntConst)op2).value}");

				dest_nonconst.StackBackValue(Register.EAX, fw, CurrentScope);
				if (stackback) fw.WriteLine("pop " + Register.EAX);
				else CurrentScope.Lock.Release(reg.Value);
			}
			else
			{
				stackback = !CurrentScope.Lock.Lock(Register.EAX);
				if (stackback)
					fw.WriteLine("push " + Register.EAX);

				bool stackbackB = CurrentScope.Lock.Locked(Register.EDX);
				if (stackbackB)
					fw.WriteLine("push " + Register.EDX);

				op1.PutValueInRegister(Register.EAX, fw, CurrentScope);
				op2.PutValueInRegister(Register.EDX, fw, CurrentScope);

				fw.Write($"imul {Register.EDX}");

				if (stackbackB) fw.WriteLine("pop " + Register.EDX);

				dest_nonconst.StackBackValue(Register.EAX, fw, CurrentScope);
				if (stackback) fw.WriteLine("pop " + Register.EAX);
				else CurrentScope.Lock.Release(Register.EAX);
			}
		}

		public override void InstrDiv(NasmHolder dest_nonconst, NasmHolder op1, NasmHolder op2)
		{
			SetLabel();
			bool stackback;

			if (op1 is NasmIntConst && op2 is NasmIntConst)
			{
				var reg = CurrentScope.Lock.LockGPR(Register.EAX);
				stackback = reg == null;
				if (stackback)
				{
					reg = Register.EAX;
					fw.WriteLine("push " + Register.EAX);
				}

				fw.WriteLine($"mov {reg}, {((NasmIntConst)op1).value / ((NasmIntConst)op2).value}");

				dest_nonconst.StackBackValue(Register.EAX, fw, CurrentScope);
				if (stackback) fw.WriteLine("pop " + Register.EAX);
				else CurrentScope.Lock.Release(reg.Value);
			}
			else
			{
				stackback = !CurrentScope.Lock.Lock(Register.EAX);
				if (stackback)
					fw.WriteLine("push " + Register.EAX);
				op1.PutValueInRegister(Register.EAX, fw, CurrentScope);

				bool stackbackB = !CurrentScope.Lock.Lock(Register.EBX);
				if (stackbackB)
					fw.WriteLine("push " + Register.EBX);
				op2.PutValueInRegister(Register.EBX, fw, CurrentScope);

				bool stackbackD = CurrentScope.Lock.Locked(Register.EDX);
				if (stackbackD)
					fw.WriteLine("push " + Register.EDX);
				fw.WriteLine($"mov {Register.EDX}, {Register.EAX}");
				fw.WriteLine($"sar {Register.EDX}, {31}");

				fw.Write($"idiv {Register.EBX}");

				if (stackbackD) fw.WriteLine("pop " + Register.EDX);

				dest_nonconst.StackBackValue(Register.EAX, fw, CurrentScope);

				if (stackback) fw.WriteLine("pop " + Register.EAX);
				else CurrentScope.Lock.Release(Register.EAX);

				if (stackbackB) fw.WriteLine("pop " + Register.EBX);
				else CurrentScope.Lock.Release(Register.EBX);
			}
		}

		public override void InstrInverse(NasmHolder dest_nonconst, NasmHolder op1)
		{
			var reg = CurrentScope.Lock.LockGPR(Register.EAX);
			bool pop = reg == null;
			if (pop)
			{
				fw.WriteLine($"push {Register.EAX}");
				reg = Register.EAX;
			}

			op1.PutValueInRegister(reg.Value, fw, CurrentScope);

			fw.WriteLine($"neg {reg}");

			dest_nonconst.StackBackValue(reg.Value, fw, CurrentScope);

			if (pop)
				fw.WriteLine($"pop {reg}");
			else
				CurrentScope.Lock.Release(reg.Value);
		}

		public override void InstrRefEq(NasmHolder dest_nonconst, NasmHolder op1, NasmHolder op2)
		{
			var reg = CurrentScope.Lock.LockGPR(Register.EAX);
			bool pop = reg == null;
			if (pop)
			{
				fw.WriteLine($"push {Register.EAX}");
				reg = Register.EAX;
			}

			var reg2 = CurrentScope.Lock.LockGPR(Register.EDX);
			bool pop2 = reg2 == null;
			if (pop2)
			{
				reg2 = reg != Register.EDX? Register.EDX : Register.EBX;
				fw.WriteLine($"push {reg2}");
			}

			op1.PutValueInRegister(reg.Value, fw, CurrentScope);
			op2.PutValueInRegister(reg2.Value, fw, CurrentScope);

			fw.WriteLine($"sub {reg}, {reg2}");
			fw.WriteLine($"mov {reg}, {0}");
			fw.WriteLine($"mov {reg2}, {1}");
			fw.WriteLine($"cmovz {reg}, {reg2}");

			dest_nonconst.StackBackValue(reg.Value, fw, CurrentScope);

			if (pop2)
				fw.WriteLine($"pop {reg2}");
			else
				CurrentScope.Lock.Release(reg2.Value);

			if (pop)
				fw.WriteLine($"pop {reg}");
			else
				CurrentScope.Lock.Release(reg.Value);
		}

		/// <summary>
		/// Returns the count of elements currently storage in the array, returns lower than o for non-array holders
		/// </summary>
		/// <remarks>
		/// array, record, string:
		/// [first mode]
		/// stack   first
		///         record
		///	| p |-> | size |     RECORD
		///			|  p1  | -> |field1|
		///						|field2|
		///						|......|
		/// 
		/// [second mode]
		/// stack     RECORD
		/// | p | -> | size |
		///			 |field1|
		///			 |field2|
		///          |......|
		/// </remarks>
		/// <param name="array">
		/// 
		/// </param>
		/// <param name="size">
		/// a holder to write the result
		/// </param>
		/// <returns></returns>
		public override void InstrSize(NasmHolder array, NasmHolder size)
		{
			SetLabel();
			var reg = CurrentScope.Lock.LockGPR(Register.EAX);
			bool stackback = reg == null;
			if (stackback)
			{
				fw.WriteLine("push " + Register.EAX);
				reg = Register.EAX;
			}

			array.PutValueInRegister(reg.Value, fw, CurrentScope);
			fw.WriteLine(string.Format("mov {0}, [{0}]", reg.Value));
			size.StackBackValue(reg.Value, fw, CurrentScope);

			if (stackback) fw.WriteLine("pop " + Register.EAX);
			else CurrentScope.Lock.Release(reg.Value);
		}
		#endregion

		#region [Call]

		/// <summary>
		/// Retuns the holder that will contain the value of the parameter inside the function
		/// [IMPLEMENTATION_TIP] zero base positions
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		public override NasmHolder GetParam(int position)
		{
			if (CurrentScope.ScopeType == NasmScopeType.Nested) throw new InvalidOperationException("no function scope");
			if (position < 0 || position >= CurrentScope.ArgumentsCount) throw new ArgumentException("params position exided");
			return new NasmHolder(this, CurrentScope, -(position + 4));
		}

		/// <summary>
		/// Enters in a function.
		/// </summary>
		/// <param name="function"></param>
		/// <param name="args"></param>
		/// <param name="returnval">
		/// if different of null the return value of the function will be placed there
		/// </param>
		public override void Call(NasmFunction function, NasmHolder[] args, NasmHolder returnval = null)
		{
			SetLabel();

			var reg = returnval != null ? CurrentScope.Lock.LockGPR(Register.EDX) : null;
			bool stackback = false;
			if (returnval != null && reg == null)
			{
				stackback = true;
				fw.WriteLine("push " + Register.EDX);
				reg = Register.EDX;
			}
			if (reg != null) CurrentScope.Lock.Release(reg.Value);

			// ReSharper disable once CoVariantArrayConversion
			function.Call(fw, reg, CurrentScope, args);

			if (reg != null)
			{
				returnval.StackBackValue(reg.Value, fw, CurrentScope);
				if (stackback)
				{
					fw.WriteLine("pop " + reg.Value);
					CurrentScope.Lock.Lock(reg.Value);
				}
			}

		}

		/// <summary>
		/// All function path must end with one form of return. An error will be generated if not.
		/// </summary>
		/// <param name="value"></param>
		public override void Ret(NasmHolder value = null)
		{
			SetLabel();

			bool release = false;
			if (value != null)
			{
				if (!CurrentScope.Lock.Locked(Register.EAX))
				{
					CurrentScope.Lock.Lock(Register.EAX);
					release = true;
				}
				value.PutValueInRegister(Register.EAX, fw, CurrentScope);
			}
			var curr = CurrentScope;
			while (curr != null && curr.ScopeType == NasmScopeType.Nested)
			{
				curr.WirteCloseCode(fw, false, false);
				curr = curr.Parent;
			}

			curr?.WirteCloseCode(fw, false, false);
			if (release) CurrentScope.Lock.Release(Register.EAX);
		}

		#endregion

		#region [GOTO & Labeling]

		/// <summary>
		/// [IMPLEMENTATION_TIP] jumping to unset label will not cause an error if the label is reserved
		/// [IMPLEMENTATION_TIP] no jumps to outer or inner scopes are allowed, you can get as far as the END label
		/// </summary>
		/// <param name="label"></param>
		public override void Goto(Guid label)
		{
			SetLabel();

			var scope = CurrentScope;
			while (scope != null &&	scope.DeclaringFunction == null)
			{
				if (scope.ScopeLabels.ContainsKey(label) || scope.ExpectedLabels.ContainsKey(label))
				{
					fw.WriteLine($"jmp _{label:N}");
					return;
				}

				scope.WirteCloseCode(fw, false, false);
				scope = scope.Parent;
			}

			throw new InvalidOperationException("the label was not found before reaching the " + (scope == null ? "root scope" : "function definition"));
		}

		/// <summary>
		/// [IMPLEMENTATION_TIP] jumping to unset label will not cause an error if the label is reserved
		/// [IMPLEMENTATION_TIP] no jumps to outers scopes are allowed, you can get as far as END label
		/// </summary>
		public override void GotoIfZero(Guid label, NasmHolder int_op)
		{
			if (!CurrentScope.ExpectedLabels.ContainsKey(label) && !CurrentScope.ScopeLabels.ContainsKey(label))
				Report.Add(new StaticError(0, 0, "Conditional jump out of the current scope", ErrorLevel.Internal));

			var reg = CurrentScope.Lock.LockGPR(Register.EAX);
			bool pop = reg == null;
			if (pop)
			{
				reg = Register.EAX;
				fw.WriteLine($"push {reg}");
			}

			int_op.PutValueInRegister(reg.Value, fw, CurrentScope);
			fw.WriteLine($"cmp {reg}, 0");

			if (pop)
				fw.WriteLine($"pop {reg}");
			else
				CurrentScope.Lock.Release(reg.Value);

			fw.WriteLine($"jz _{label:N}");
		}

		/// <summary>
		/// [IMPLEMENTATION_TIP] jumping to unset label will not cause an error if the label is reserved
		/// [IMPLEMENTATION_TIP] no jumps to outers scopes are allowed, you can get as far as END label
		/// </summary>
		public override void GotoIfNotZero(Guid label, NasmHolder int_op)
		{
			if (!CurrentScope.ExpectedLabels.ContainsKey(label) && !CurrentScope.ScopeLabels.ContainsKey(label))
				Report.Add(new StaticError(0, 0, "Conditional jump out of the current scope", ErrorLevel.Internal));

			var reg = CurrentScope.Lock.LockGPR(Register.EAX);
			bool pop = reg == null;
			if (pop)
			{
				reg = Register.EAX;
				fw.WriteLine($"push {reg}");
			}

			int_op.PutValueInRegister(reg.Value, fw, CurrentScope);
			fw.WriteLine($"cmp {reg}, 0");

			if (pop)
				fw.WriteLine($"pop {reg}");
			else
				CurrentScope.Lock.Release(reg.Value);

			fw.WriteLine($"jnz _{label:N}");
		}

		/// <summary>
		/// [IMPLEMENTATION_TIP] jumping to unset label will not cause an error if the label is reserved
		/// [IMPLEMENTATION_TIP] no jumps to outers scopes are allowed, you can get as far as END label
		/// [IMPLEMENTATION_TIP] int_op >= 0
		/// </summary>
		public override void GotoIfNotNegative(Guid label, NasmHolder int_op)
		{
			if (!CurrentScope.ExpectedLabels.ContainsKey(label) && !CurrentScope.ScopeLabels.ContainsKey(label))
				Report.Add(new StaticError(0, 0, "Conditional jump out of the current scope", ErrorLevel.Internal));

			var reg = CurrentScope.Lock.LockGPR(Register.EAX);
			bool pop = reg == null;
			if (pop)
			{
				reg = Register.EAX;
				fw.WriteLine($"push {reg}");
			}

			int_op.PutValueInRegister(reg.Value, fw, CurrentScope);
			fw.WriteLine($"cmp {reg}, 0");

			if (pop)
				fw.WriteLine($"pop {reg}");
			else
				CurrentScope.Lock.Release(reg.Value);

			fw.WriteLine($"jge _{label:N}");
		}

		/// <summary>
		/// [IMPLEMENTATION_TIP] jumping to unset label will not cause an error if the label is reserved
		/// [IMPLEMENTATION_TIP] no jumps to outers scopes are allowed, you can get as far as END label
		/// [IMPLEMENTATION_TIP] int_op less than 0
		/// </summary>
		public override void GotoIfNegative(Guid label, NasmHolder int_op)
		{
			if (!CurrentScope.ExpectedLabels.ContainsKey(label) && !CurrentScope.ScopeLabels.ContainsKey(label))
				Report.Add(new StaticError(0, 0, "Conditional jump out of the current scope", ErrorLevel.Internal));

			var reg = CurrentScope.Lock.LockGPR(Register.EAX);
			bool pop = reg == null;
			if (pop)
			{
				reg = Register.EAX;
				fw.WriteLine($"push {reg}");
			}

			int_op.PutValueInRegister(reg.Value, fw, CurrentScope);
			fw.WriteLine($"cmp {reg}, 0");

			if (pop)
				fw.WriteLine($"pop {reg}");
			else
				CurrentScope.Lock.Release(reg.Value);

			fw.WriteLine($"jl _{label:N}");
		}

		#endregion

		#region [Scope Handling]

		/// <summary>
		/// </summary>
		/// <param name="definetype">true if the new scope will contains type definitions</param>
		/// <param name="namehint">
		/// [IMPLEMENTATION_TIP] adds a comment befor enter the scope on the compiled code
		/// </param>
		[ScopeChanger(Reason = "Creates and enters in a nested scope", ScopeName = "InnerScope_<scopelabel>")]
		public override void EnterNestedScope(bool definetype = false, string namehint = null)
		{
			SetLabel();

			CurrentScope = new NasmEmitterScope(CurrentScope, g.GNext(), g.GNext(), g.GNext(), g.GNext());

			if (!string.IsNullOrWhiteSpace(namehint)) fw.WriteLine(";" + namehint);
			fw.WriteLine($"_{CurrentScope.BeforeEnterScope:N}:");
			fw.IncrementIndentation();
			CurrentScope.WriteEnteringCode(fw);
			fw.WriteLine($"_{CurrentScope.BiginScope:N}:");
        }

		/// <summary>
		/// [IMPLEMENTATION_TIP] This should free any memory reserved in the scope that is about to leave.
		/// [IMPLEMENTATION_TIP] Unused labels at the end of the scope will generate warnings
		/// [IMPLEMENTATION_TIP] Unset label at the end of the scope generate critical errors
		/// </summary>
		[ScopeChanger(Reason = "Closes the current scope and returns to it's parent")]
		public override void LeaveScope()
		{
			fw.WriteLine($"_{CurrentScope.EndScope:N}:");
			if (SetLabel() && CurrentScope.ScopeType != NasmScopeType.Nested)
				fw.WriteLine("nop");

			if (CurrentScope.ScopeType == NasmScopeType.Nested)
				CurrentScope.WirteCloseCode(fw, true, false);

			fw.DecrementIndentation();
			fw.WriteLine($"_{CurrentScope.AfterEndScope:N}:");
			var f = CurrentScope.DeclaringFunction;
			var label = CurrentScope.BeforeEnterScope;
			CurrentScope = CurrentScope.Parent;

			if (f != null)
			{
				var reg = CurrentScope.Lock.LockGPR(Register.EAX);
				bool stackback = reg == null;
				if (stackback)
				{
					fw.WriteLine("push " + Register.EAX);
					reg = Register.EAX;
				}

				NasmFunction.AlocateFunction(fw, reg.Value, CurrentScope, this, label);
				f.StackBackValue(reg.Value, fw, CurrentScope);

				if (stackback) fw.WriteLine("pop " + Register.EAX);
				else CurrentScope.Lock.Release(reg.Value);
			}
		}

		#endregion

		#region [Helpers]

		public static void EmitError(FormatWriter fw, NasmEmitterScope acceding, NasmEmitter bound, int code, string message = null)
		{
			var l = acceding.Lock;
			acceding.Lock = new RegisterLock();

			fw.WriteLine($";emitting error {code}{(message != null? ": " + message : "")}");
			fw.IncrementIndentation();
			var ret = bound.AddConstant(code);
			if (message != null)
			{
				var ms = bound.AddConstant(message);
				bound.PrintS.Call(fw, null, acceding, ms);
			}

			var curr = acceding;
			while (curr != null && curr.ScopeType == NasmScopeType.Nested)
			{
				curr.WirteCloseCode(fw, false, true);
				curr = curr.Parent;
			}

			curr?.WirteCloseCode(fw, false, true, ret);
			fw.DecrementIndentation();
			acceding.Lock = l;
		}

		public bool SetLabel()
		{
			BlankLine();
			if (string.IsNullOrEmpty(nexlabelcomment)) return false;

			fw.WriteLine(";" + nexlabelcomment);
			fw.WriteLine($"_{NextInstructionLabel:N}:");

			CurrentScope.ScopeLabels.Add(NextInstructionLabel, nexlabelcomment);

            nexlabelcomment = null;
			NextInstructionLabel = Guid.Empty;
			return true;
		}

		/// <summary>
		/// Ends in a label
		/// </summary>
		public static void CatchAndRethrow(FormatWriter fw, NasmEmitterScope acceding, NasmEmitter bound)
		{

			fw.IncrementIndentation();
			fw.WriteLine($"cmp {Register.ECX}, dword {ErrorCode}");
			var ndoit = bound.g.GNext();
			fw.WriteLine($"jne _{ndoit:N}");

			var curr = acceding;
			while (curr != null && curr.ScopeType == NasmScopeType.Nested)
			{
				curr.WirteCloseCode(fw, false, false);
				curr = curr.Parent;
			}

			curr?.WirteCloseCode(fw, false, true);
			fw.DecrementIndentation();
			fw.WriteLine($"_{ndoit:N}:");
		}

		public void AddExtern(string exf)
		{
			Externs.Add(exf);
		}

		string STD()
		{
			FormatWriter f = new FormatWriter();

			foreach (var item in std)
			{
				switch (item.Key.ToLower())
				{
					case "prints":
						AddPrintS(f, this);
						break;

					case "printi":
						AddPrintI(f);
						break;

					default:
						throw new ArgumentException("No definition for " + item.Key);
				}
			}

			return f.Flush();
		}

		static void AddPrintS(FormatWriter fw, NasmEmitter bound)
		{
			fw.WriteLine(PrintSFunctionLabel + ":");
			fw.IncrementIndentation();
			fw.WriteLine("mov EAX, [ESP + 4]");
			fw.WriteLine("cmp EAX, 0");
			fw.WriteLine("je .null_error_exit");
			fw.WriteLine("add EAX, 4");//size space
			fw.WriteLine("push EAX");
			fw.WriteLine($"push dword {PrintSFormatName}");
			fw.WriteLine("call _printf");
			fw.WriteLine("add ESP, 8");
			fw.WriteLine("xor EAX, EAX");
			fw.WriteLine("xor ECX, ECX");
			fw.WriteLine("ret");
			fw.WriteLine("");
			fw.WriteLine(".null_error_exit:");
			//TODO: crear un diccionario con los errores y los codigos
			var m = bound.AddConstant("Null Reference");
			m.PutValueInRegister(Register.EAX, fw, null);
			fw.WriteLine("push EAX");
			fw.WriteLine($"call {PrintSFunctionLabel}");
			fw.WriteLine("add ESP, 4");
			fw.WriteLine("mov EAX, 3");
			fw.WriteLine($"mov ECX, {ErrorCode}");
			fw.WriteLine("ret");
			fw.DecrementIndentation();
		}

		static void AddPrintI(FormatWriter fw)
		{
			fw.WriteLine(PrintIFunctionLabel + ":");
			fw.IncrementIndentation();
			fw.WriteLine("mov EAX, [ESP + 4]");
			fw.WriteLine("push EAX");
			fw.WriteLine($"push dword {PrintIFormatName}");
			fw.WriteLine("call _printf");
			fw.WriteLine("add ESP, 8");
			fw.WriteLine("xor EAX, EAX");
			fw.WriteLine("xor ECX, ECX");
			fw.WriteLine("ret");
			fw.WriteLine("");
			fw.DecrementIndentation();
		}

		/// <summary>
		/// Gets the bounded function that will allocate a new object of this type
		/// Array Case
		/// parameters: left -> right = top -> down
		/// 1 IHolder : int -> element count
		/// 2 IHolder : ArrayType -> default value
		/// returns :
		/// IHolder -> array
		/// </summary>
		static void ArrayAllocator(FormatWriter fw, NasmEmitter bound, NasmEmitterScope acceding, Register[] args)
		{

			// ECX -> legth
			// EAX -> Default value
			fw.IncrementIndentation();
			Guid ndoit = bound.g.GNext();
			Guid zero = bound.g.GNext();

			//<error checking>
			fw.WriteLine($"cmp {Register.ECX}, 0");
			fw.WriteLine($"jge _{ndoit:N}");
			EmitError(fw, acceding, bound, 2, "Array size must be non-negative");
			fw.WriteLine($"_{ndoit:N}:");
			//</error checking>

			bool stackback = false;
			var reg = acceding.Lock.LockGPR(Register.EBX);
			if (reg == null)
			{
				stackback = true;
				reg = Register.EBX;
				fw.WriteLine($"push {Register.EBX}");
			}
			fw.WriteLine($";push {reg.Value}");

			fw.WriteLine($"push {Register.EAX}");
			fw.WriteLine($"push {Register.ECX}");

			acceding.Lock.Release(Register.EAX);
			acceding.Lock.Release(Register.ECX);

			fw.WriteLine($"inc {Register.ECX}");
			fw.WriteLine($"shl {Register.ECX}, {2}");
			NasmFunction.Malloc.Call(fw, reg, acceding, new NasmRegisterHolder(bound, Register.ECX));


			fw.WriteLine($"pop {Register.ECX}");
			fw.WriteLine($"pop {Register.EAX}");
			acceding.Lock.Lock(Register.EAX);
			acceding.Lock.Lock(Register.ECX);

			fw.WriteLine($"mov {Register.EDI}, {reg.Value}");
			fw.WriteLine($"mov [{Register.EDI}], {Register.ECX}");
			fw.WriteLine($"add {Register.EDI}, {4}");

			fw.WriteLine($"cmp {Register.ECX}, 0");
			fw.WriteLine($"je _{zero:N}");

			fw.WriteLine("cld");
			fw.WriteLine("rep stosd");

			fw.WriteLine($"mov {Register.ECX}, {reg.Value}");

			if (stackback) fw.WriteLine($"pop {reg.Value}");
			else
				acceding.Lock.Release(reg.Value);

			fw.DecrementIndentation();
			fw.WriteLine($"_{zero:N}:");
		}

		/// <summary>
		/// Gets the bounded function that will allocate a new object of this type
		/// Array Case
		/// parameters: left -> right = top -> down
		/// 1 IHolder : int -> element count
		/// 2 IHolder : ArrayType -> default value
		/// returns :
		/// IHolder -> array
		/// </summary>
		static void ArrayAllocatorBytesZeroEnd(FormatWriter fw, NasmEmitter bound, NasmEmitterScope acceding, Register[] args)
		{
			// ECX -> legth
			// EAX -> Default value
			fw.IncrementIndentation();
			Guid ndoit = bound.g.GNext();
			Guid zero = bound.g.GNext();

			//<error checking>
			fw.WriteLine($"cmp {Register.ECX}, 0");
			fw.WriteLine($"jge _{ndoit:N}");
			EmitError(fw, acceding, bound, 2, "Array size must be non-negative");
			fw.WriteLine($"_{ndoit:N}:");
			//</error checking>

			bool stackback = false;
			var reg = acceding.Lock.LockGPR(Register.EBX);
			if (reg == null)
			{
				stackback = true;
				reg = Register.EBX;
				fw.WriteLine($"push {Register.EBX}");
			}
			fw.WriteLine($";push {reg.Value}");

			fw.WriteLine($"push {Register.EAX}");
			fw.WriteLine($"push {Register.ECX}");

			acceding.Lock.Release(Register.EAX);
			acceding.Lock.Release(Register.ECX);

			fw.WriteLine($"add {Register.ECX}, 5");
			NasmFunction.Malloc.Call(fw, reg, acceding, new NasmRegisterHolder(bound, Register.ECX));

			fw.WriteLine($"pop {Register.ECX}");
			fw.WriteLine($"pop {Register.EAX}");
			acceding.Lock.Lock(Register.EAX);
			acceding.Lock.Lock(Register.ECX);

			fw.WriteLine($"mov {Register.EDI}, {reg.Value}");
			fw.WriteLine($"mov [{Register.EDI}], {Register.ECX}");
			fw.WriteLine($"add {Register.EDI}, {4}");

			fw.WriteLine($"cmp {Register.ECX}, 0");
			fw.WriteLine($"je _{zero:N}");

			fw.WriteLine("cld");
			fw.WriteLine("rep stosb");

			fw.WriteLine($"mov {Register.ECX}, {reg.Value}");

			if (stackback) fw.WriteLine($"pop {reg.Value}");
			else
				acceding.Lock.Release(reg.Value);

			fw.WriteLine($"_{zero:N}:");
			fw.WriteLine(string.Format("xor {0}, {0}", Register.EAX.ByteVersion()));
			fw.WriteLine($"mov [{Register.EDI}], {Register.EAX.ByteVersion()}");
			fw.DecrementIndentation();
		}

		/// <summary>
		/// parameters:
		/// IHolder : int -> element index
		/// IHolder : instance
		/// returns:
		/// member value
		/// 
		/// throws IndexOutOfRange Error
		/// </summary>
		static void MemberReadAccess(FormatWriter fw, NasmEmitter bound, NasmEmitterScope acceding, Register[] args)
		{
			Guid IORexcept = bound.g.GNext();
			Guid code = bound.g.GNext();
			Guid passnull = bound.g.GNext();
			fw.IncrementIndentation();

			//Null Reference
			fw.WriteLine($"cmp {args[0]}, {Null}");
			fw.WriteLine($"jne _{passnull:N}");

			EmitError(fw, acceding, bound, 3, "Null Refernce");

			fw.WriteLine($"_{passnull:N}:");

			//Index out of range
			fw.WriteLine($"cmp {args[1]}, 0");
			fw.WriteLine($"jl _{IORexcept:N}");
			fw.WriteLine($"cmp {args[1]}, [{args[0]}]");
			fw.WriteLine($"jge _{IORexcept:N}");

			fw.WriteLine($"jmp _{code:N}");
			fw.WriteLine($"_{IORexcept:N}:");
			EmitError(fw, acceding, bound, 1, "Index out of range");

			fw.WriteLine($"_{code:N}:");
			fw.WriteLine("inc " + args[1]);
			fw.WriteLine($"shl {args[1]}, 2");
			fw.WriteLine($"add {args[0]}, {args[1]}");
			fw.WriteLine(string.Format("mov {0}, [{0}]", args[0]));
			fw.DecrementIndentation();
		}

		static void MemberReadAccessByteString(FormatWriter fw, NasmEmitter bound, NasmEmitterScope acceding, Register[] args)
		{
			Guid IORexcept = bound.g.GNext();
			Guid code = bound.g.GNext();
			Guid passnull = bound.g.GNext();
			fw.IncrementIndentation();

			//Null Reference
			fw.WriteLine($"cmp {args[0]}, {Null}");
			fw.WriteLine($"jne _{passnull:N}");

			EmitError(fw, acceding, bound, 3, "Null Refernce");

			fw.WriteLine($"_{passnull:N}:");

			//Index out of range
			fw.WriteLine($"cmp {args[1]}, 0");
			fw.WriteLine($"jl _{IORexcept:N}");
			fw.WriteLine($"cmp {args[1]}, [{args[0]}]");
			fw.WriteLine($"jge _{IORexcept:N}");

			fw.WriteLine($"jmp _{code:N}");
			fw.WriteLine($"_{IORexcept:N}:");
			EmitError(fw, acceding, bound, 1, "Index out of range");

			fw.WriteLine($"_{code:N}:");
			fw.WriteLine($"add {args[1]}, 4");
			fw.WriteLine($"add {args[0]}, {args[1]}");
			fw.WriteLine($"mov {args[0].ByteVersion()}, [{args[0]}]");
			fw.WriteLine($"movzx {args[0]}, {args[0].ByteVersion()}");
			fw.DecrementIndentation();
		}

		/// <summary>
		/// parameters:
		/// IHolder : source
		/// IHolder : int -> element index
		/// IHolder : instance
		/// returns:
		/// void
		/// 
		/// throws IndexOutOfRange Error
		/// </summary>
		static void MemberWriteAccess(FormatWriter fw, NasmEmitter bound, NasmEmitterScope acceding, Register[] args)
		{
			Guid IORexcept = bound.g.GNext();
			Guid code = bound.g.GNext();
			Guid passnull = bound.g.GNext();
			fw.IncrementIndentation();

			//Null Reference
			fw.WriteLine($"cmp {args[0]}, {Null}");
			fw.WriteLine($"jne _{passnull:N}");

			EmitError(fw, acceding, bound, 3, "Null Refernce");

			fw.WriteLine($"_{passnull:N}:");

			//Index out of range
			fw.WriteLine($"cmp {args[1]}, 0");
			fw.WriteLine($"jl _{IORexcept:N}");
			fw.WriteLine($"cmp {args[1]}, [{args[0]}]");
			fw.WriteLine($"jge _{IORexcept:N}");

			fw.WriteLine($"jmp _{code:N}");
			fw.WriteLine($"_{IORexcept:N}:");
			EmitError(fw, acceding, bound, 1, "Index out of range");

			fw.WriteLine($"_{code:N}:");
			fw.WriteLine("inc " + args[1]);
			fw.WriteLine($"shl {args[1]}, 2");
			fw.WriteLine($"add {args[0]}, {args[1]}");
			fw.WriteLine($"mov [{args[0]}], {args[2]}");
			fw.DecrementIndentation();
		}

		void AddExterns()
		{
			foreach (var item in std)
			{
				switch (item.Key)
				{
					case "prints":
					case "printi":
						Externs.Add("_printf");
						break;

					case "getchar":
						Externs.Add("_scanf");
						break;
					//default:
						//break;
				}
			}
		}
		#endregion
	}

}
