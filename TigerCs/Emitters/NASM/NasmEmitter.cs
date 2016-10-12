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

		public const int ErrorCode = 0xe7707;

		[InArgument(Comment = "the name of the output file, empty for standar output", ConsoleShortName = "-o", DefaultValue ="out.asm")]
		public string OutputFile { get; set; }

		FormatWriter fw;
		ErrorReport r;

		TextWriter t;
		FileStream toclose;

		Dictionary<string, NasmFunction> std;
		HashSet<string> Externs;
		Dictionary<string, NasmStringConst> StringConst;
		int StringConstEnd;
		NasmFunction PrintS;

		public NasmEmitter()
		{
			fw = new FormatWriter();
			Externs = new HashSet<string>();
			std = new Dictionary<string, NasmFunction>();

			PrintS = new NasmCFunction(PrintSFunctionLabel, false, this, name: "PrintS");
			std["prints"] = PrintS;

			NasmType.Int = new NasmType(NasmRefType.None);
			NasmType.String = new NasmType(NasmRefType.Dynamic, -1);
			NasmType.QuadWordRMemberAccess = new NasmMacroFunction(MemberReadAccess, this, "MemberReadAccess");
			NasmType.QuadWordWMemberAccess = new NasmMacroFunction(MemberWriteAccess, this,"MemberWriteAccess");
			NasmType.ByteRMemberAccess = new NasmMacroFunction(MemberReadAccessByteString, this, "MemberReadAccess(Bytes)");
			NasmType.ArrayAllocator = new NasmMacroFunction(ArrayAllocator, this, "ArrayAllocator") { Requested = new[] { Register.ECX, Register.EAX } };
			NasmType.ByteZeroEndArrayAllocator = new NasmMacroFunction(ArrayAllocatorBytesZeroEnd, this, "ArrayAllocator") { Requested = new[] { Register.ECX, Register.EAX } };
		}

		public override void InitializeCodeGeneration(ErrorReport report)
		{
			if (string.IsNullOrWhiteSpace(OutputFile))
				t = Console.Out;
			else
			{
				toclose = new FileStream(OutputFile, FileMode.Create, FileAccess.Write);
				t = new StreamWriter(toclose);

			}

			r = report;
			StringConst = new Dictionary<string, NasmStringConst>();
			StringConstEnd = 0;

			fw.WriteLine("%include \"io.inc\"");
			fw.WriteLine("section .data");
			fw.WriteLine(string.Format("{0} db '%', 's', 0", PrintSFormatName));
			fw.WriteLine(string.Format("{0} db '%', 'i', 0", PrintIFormatName));
			fw.WriteLine(string.Format("{1}{0}{2}", fw.IndexOfFormat, '{', '}'), (Func<string>)(() =>
			{
				if (StringConst.Count == 0) return ";no string const\n";
				StringBuilder sb = new StringBuilder();
				sb.Append(string.Format("{0} db ", StringConstName));

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
					sb.Append(string.Format("extern {0}\n", ex));
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
			return new NasmIntConst(value);
		}
		public override NasmHolder AddConstant(string value)
		{
			NasmStringConst svar;
			if (StringConst.TryGetValue(value, out svar)) return svar;

			svar = new NasmStringConst(value, StringConstName, StringConstEnd);
			StringConst[value] = svar;
			StringConstEnd += value.Length + 5;// 4 for size and 1 for \0
			return svar;
		}
		public override NasmHolder BindVar(NasmType type, NasmHolder defaultvalue = null, string name = null, bool global = false)
		{
			NasmHolder v;
			if (name != null || CurrentScope.ReleasedTempVars.Count <= 0)
			{
				fw.WriteLine(string.Format("; {0}<EBP - {1}>", name, (CurrentScope.VarsCount + 1) * 4));
				v = new NasmHolder(CurrentScope, CurrentScope.VarsCount);
				CurrentScope.VarsCount++;
			}
			else
				v = new NasmHolder(CurrentScope, CurrentScope.ReleasedTempVars.Dequeue());

			if (defaultvalue == null)
				defaultvalue = AddConstant(0);

			InstrAssing(v, defaultvalue);
			
			return v;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="op1"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		public override NasmHolder StaticMemberAcces(NasmType tigertype, NasmHolder op1, int index)
		{
			if (index < 0)
				throw new IndexOutOfRangeException("index must be non negative");
			bool dynamicupperboundcheck = false;
			switch (tigertype.RefType)
			{
				case NasmRefType.None:
					throw new InvalidOperationException("the provided type has no members to be referenced");
				case NasmRefType.Fixed:
					if (index >= tigertype.AsRefSize)
						throw new IndexOutOfRangeException("index exceed members count");
					break;
				case NasmRefType.Dynamic:
				case NasmRefType.NoSet:
				default:
					dynamicupperboundcheck = true;
					break;
			}
			return new NasmReference(op1, index, this, CurrentScope, ReferenceEquals(tigertype, NasmType.String) ? WordSize.Byte : WordSize.DWord, dynamicupperboundcheck);
		}
		#endregion

		#region [Functions]
		[ScopeChanger(Reason = "Creates and enters in the primary scope of the program, this has no parent scope after closing it no fouther instructions can be emitted", ScopeName = "Main")]
		public override NasmFunction EntryPoint(bool returns = false, bool stringparams = false)
		{
			CurrentScope = new NasmEmitterScope(null, g.GNext(), g.GNext(), g.GNext(), g.GNext(), NasmScopeType.CFunction, 2);
			
			fw.WriteLine(";Main");
			fw.WriteLine("CMAIN:");
			fw.WriteLine(string.Format("_{0}:", CurrentScope.BeforeEnterScope.ToString("N")));
			fw.IncrementIndentation();
			CurrentScope.WriteEnteringCode(fw);
			fw.WriteLine(string.Format("_{0}:", CurrentScope.BiginScope.ToString("N")));

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
			fw.WriteLine(string.Format("jmp _{0}", CurrentScope.AfterEndScope.ToString("N")));
			fw.WriteLine(string.Format("_{0}:", CurrentScope.BeforeEnterScope.ToString("N")));
			fw.IncrementIndentation();
			CurrentScope.WriteEnteringCode(fw);
			fw.WriteLine(string.Format("_{0}:", CurrentScope.BiginScope.ToString("N")));
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
			fw.WriteLine("");
			fw.WriteLine(";record " + aheadedtype.Name);
			fw.WriteLine(string.Format("jmp _{0}", CurrentScope.AfterEndScope.ToString("N")));
			fw.WriteLine(string.Format("_{0}:", CurrentScope.BeforeEnterScope.ToString("N")));
			fw.IncrementIndentation();			

			fw.WriteLine(string.Format("mov {0}, {1}", Register.ECX, (members.Length + 1) * 4));
			fw.WriteLine("push " + Register.ECX);
			fw.WriteLine("call " + MallocLabel);
			fw.WriteLine(string.Format("add {0}, 4 ", Register.ESP));

			fw.WriteLine(string.Format("mov {0}, {1}", Register.ECX, members.Length));
			fw.WriteLine(string.Format("move [{0}], {1}", Register.EAX, Register.ECX));
			fw.WriteLine(string.Format("move {0}, {1}", Register.EDI, Register.EAX));
			fw.WriteLine(string.Format("add {0}, {1}", Register.EDI, 4));

			fw.WriteLine(string.Format("move {0}, {1}", Register.ESI, Register.ESP));
			fw.WriteLine(string.Format("add {0}, {1}", Register.ESI, 4));

			fw.WriteLine("cdl");
			fw.WriteLine("rep movsd");

			fw.WriteLine("ret");
			fw.DecrementIndentation();
			fw.WriteLine(string.Format("_{0}:", CurrentScope.AfterEndScope.ToString("N")));

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

		public override bool TryBindSTDConst(string name, out NasmHolder constant){ throw new NotImplementedException(); }

		#endregion

		#endregion

		#region [General Instructions]

		/// <summary>
		/// </summary>
		/// <param name="dest_nonconst"></param>
		/// <param name="value"></param>
		/// <param name="dest_as_pointer"></param>
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

		public override void InstrAdd(NasmHolder dest_nonconst, NasmHolder op1, NasmHolder op2){ throw new NotImplementedException(); }
		public override NasmHolder InstrAdd_TempBound(NasmHolder op1, NasmHolder op2){ throw new NotImplementedException(); }

		public override void InstrSub(NasmHolder dest_nonconst, NasmHolder op1, NasmHolder op2){ throw new NotImplementedException(); }
		public override NasmHolder InstrSub_TempBound(NasmHolder op1, NasmHolder op2){ throw new NotImplementedException(); }

		public override void InstrMult(NasmHolder dest_nonconst, NasmHolder op1, NasmHolder op2){ throw new NotImplementedException(); }
		public override NasmHolder InstrMult_TempBound(NasmHolder op1, NasmHolder op2){ throw new NotImplementedException(); }

		public override void InstrDiv(NasmHolder dest_nonconst, NasmHolder op1, NasmHolder op2){ throw new NotImplementedException(); }
		public override NasmHolder InstrDiv_TempBound(NasmHolder op1, NasmHolder op2){ throw new NotImplementedException(); }

		public override void InstrInverse(NasmHolder dest_nonconst, NasmHolder op1){ throw new NotImplementedException(); }
		public override NasmHolder InstrInverse_TempBound(NasmHolder op1){ throw new NotImplementedException(); }

		public override void InstrRefEq(NasmHolder dest_nonconst, NasmHolder op1, NasmHolder op2){ throw new NotImplementedException(); }
		public override NasmHolder InstrRefEq_TempBound(NasmHolder op1, NasmHolder op2){ throw new NotImplementedException(); }

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
			return new NasmHolder(CurrentScope, -(position + 4));
		}

		/// <summary>
		/// Enters in a function.
		/// </summary>
		/// <param name="function"></param>
		/// <param name="args"></param>
		/// <param name="returnval">
		/// if diferent of null the return value of the function will be placed there
		/// </param>
		public override void Call(NasmFunction function, NasmHolder[] args, NasmHolder returnval = null)
		{
			var reg = returnval != null ? CurrentScope.Lock.LockGPR(Register.EDX) : null;
			bool stackback = false;
			if (returnval != null && reg == null)
			{
				stackback = true;
				fw.WriteLine("push " + Register.EDX);
				reg = Register.EDX;
			}
			if (reg != null) CurrentScope.Lock.Release(reg.Value);

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
			SetLabel();
			var curr = CurrentScope;
			while (curr != null && curr.ScopeType == NasmScopeType.Nested)
			{
				curr.WirteCloseCode(fw, false, false);
				curr = curr.Parent;
			}

			if (curr != null) curr.WirteCloseCode(fw, false, false);
			if (release) CurrentScope.Lock.Release(Register.EAX);
		}

		#endregion

		#region [GOTO & Labeling]

		/// <summary>
		/// Scope dependent label, before enter the scope
		/// </summary>
		//public override Guid BeforeEnterScope { get{ } }

		/// <summary>
		/// Scope dependent label, first instruction of the current scope
		/// </summary>
		//public override Guid BiginScope { get; }

		/// <summary>
		/// Scope dependent label, before exiting the scope
		/// </summary>
		//public override Guid EndScope { get; }

		/// <summary>
		/// [IMPLEMENTATION_TIP] On labels collision returns the actual label
		/// [IMPLEMENTATION_TIP] if the label for the next instruction is already setted will return the setted label
		/// </summary>
		/// <param name="label"></param>
		/// <returns></returns>
		//public override Guid SetLabelToNextInstruction(string label){ throw new NotImplementedException(); }
		//public override Guid ReserveInstructionLabel(string label){ throw new NotImplementedException(); }
		//public override void ApplyReservedLabel(Guid reservedlabel){ throw new NotImplementedException(); }

		/// <summary>
		/// [IMPLEMENTATION_TIP] jumping to unset label will not cause an error if the label is reserved
		/// [IMPLEMENTATION_TIP] no jumps to outer or inner scopes are allowed, you can get as far as the END label
		/// </summary>
		/// <param name="label"></param>
		public override void Goto(Guid label){ throw new NotImplementedException(); }

		public override void UnstructuredGoto(Guid abslabel){ throw new NotImplementedException(); }

		/// <summary>
		/// [IMPLEMENTATION_TIP] jumping to unset label will not cause an error if the label is reserved
		/// [IMPLEMENTATION_TIP] no jumps to outers scopes are allowed, you can get as far as END label
		/// </summary>
		/// <param name="label"></param>
		public override void GotoIfZero(Guid label, NasmHolder int_op){ throw new NotImplementedException(); }

		/// <summary>
		/// [IMPLEMENTATION_TIP] jumping to unset label will not cause an error if the label is reserved
		/// [IMPLEMENTATION_TIP] no jumps to outers scopes are allowed, you can get as far as END label
		/// </summary>
		/// <param name="label"></param>
		public override void GotoIfNotZero(Guid label, NasmHolder int_op){ throw new NotImplementedException(); }

		/// <summary>
		/// [IMPLEMENTATION_TIP] jumping to unset label will not cause an error if the label is reserved
		/// [IMPLEMENTATION_TIP] no jumps to outers scopes are allowed, you can get as far as END label
		/// [IMPLEMENTATION_TIP] int_op >= 0
		/// </summary>
		/// <param name="label"></param>
		public override void GotoIfNotNegative(Guid label, NasmHolder int_op){ throw new NotImplementedException(); }

		/// <summary>
		/// [IMPLEMENTATION_TIP] jumping to unset label will not cause an error if the label is reserved
		/// [IMPLEMENTATION_TIP] no jumps to outers scopes are allowed, you can get as far as END label
		/// [IMPLEMENTATION_TIP] int_op less than 0
		/// </summary>
		/// <param name="label"></param>
		public override void GotoIfNegative(Guid label, NasmHolder int_op){ throw new NotImplementedException(); }

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
			CurrentScope = new NasmEmitterScope(CurrentScope, g.GNext(), g.GNext(), g.GNext(), g.GNext(), NasmScopeType.Nested);

			if (!string.IsNullOrWhiteSpace(namehint)) fw.WriteLine(";" + namehint);
			fw.WriteLine(string.Format("_{0}:", CurrentScope.BeforeEnterScope.ToString("N")));
			fw.IncrementIndentation();
			CurrentScope.WriteEnteringCode(fw);
			fw.WriteLine(string.Format("_{0}:", CurrentScope.BiginScope.ToString("N")));
        }

		/// <summary>
		/// [IMPLEMENTATION_TIP] This should free any memory reserved in the scope that is about to leave.
		/// [IMPLEMENTATION_TIP] Unused labels at the end of the scope will generate warnings
		/// [IMPLEMENTATION_TIP] Unset label at the end of the scope generate critical errors
		/// </summary>
		[ScopeChanger(Reason = "Closes the current scope and returns to it's parent")]
		public override void LeaveScope()
		{
			fw.WriteLine(string.Format("_{0}:", CurrentScope.EndScope.ToString("N")));
			if (SetLabel() && CurrentScope.ScopeType != NasmScopeType.Nested)
				fw.WriteLine("nop");

			if (CurrentScope.ScopeType == NasmScopeType.Nested)
				CurrentScope.WirteCloseCode(fw, true, false);

			fw.DecrementIndentation();
			fw.WriteLine(string.Format("_{0}:", CurrentScope.AfterEndScope.ToString("N")));
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

			fw.WriteLine(string.Format(";emitting error {0}{1}", code, message != null ? ": " + message : ""));
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

			if (curr != null) curr.WirteCloseCode(fw, false, true, ret);
			fw.DecrementIndentation();
			acceding.Lock = l;
		}

		public bool SetLabel()
		{
			if (string.IsNullOrEmpty(nexlabelcomment)) return false;

			fw.WriteLine(";" + nexlabelcomment);
			fw.WriteLine(string.Format("_{0}:", NextInstructionLabel.ToString("N")));

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
			fw.WriteLine(string.Format("cmp {0}, dword {1}", Register.ECX, ErrorCode));
			var ndoit = bound.g.GNext();
			fw.WriteLine(string.Format("jne _{0}", ndoit.ToString("N")));

			var curr = acceding;
			while (curr != null && curr.ScopeType == NasmScopeType.Nested)
			{
				curr.WirteCloseCode(fw, false, false);
				curr = curr.Parent;
			}

			if (curr != null) curr.WirteCloseCode(fw, false, true);
			fw.DecrementIndentation();
			fw.WriteLine(string.Format("_{0}:", ndoit.ToString("N")));
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
						AddPrintS(f);
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

		static void AddPrintS(FormatWriter fw)
		{
			fw.WriteLine(PrintSFunctionLabel + ":");
			fw.IncrementIndentation();
			fw.WriteLine("mov EAX, [ESP + 4]");
			fw.WriteLine("add EAX, 4");//size space
			fw.WriteLine("push EAX");
			fw.WriteLine(string.Format("push dword {0}", PrintSFormatName));
			fw.WriteLine("call _printf");
			fw.WriteLine("add ESP, 8");
			fw.WriteLine("xor EAX, EAX");
			fw.WriteLine("xor ECX, ECX");
			fw.WriteLine("ret");
			fw.WriteLine("");
			fw.DecrementIndentation();
		}

		static void AddPrintI(FormatWriter fw)
		{
			fw.WriteLine(PrintIFunctionLabel + ":");
			fw.IncrementIndentation();
			fw.WriteLine("mov EAX, [ESP + 4]");
			fw.WriteLine("push EAX");
			fw.WriteLine(string.Format("push dword {0}", PrintIFormatName));
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
			fw.WriteLine(string.Format("cmp {0}, 0", Register.ECX));
			fw.WriteLine(string.Format("jge _{0}", ndoit.ToString("N")));
			EmitError(fw, acceding, bound, 2, "Array size must be non-negative");
			fw.WriteLine(string.Format("_{0}:", ndoit.ToString("N")));
			//</error checking>

			bool stackback = false;
			var reg = acceding.Lock.LockGPR(Register.EBX);
			if (reg == null)
			{
				stackback = true;
				reg = Register.EBX;
				fw.WriteLine(string.Format("push {0}", Register.EBX));
			}
			fw.WriteLine(string.Format(";push {0}", reg.Value));

			fw.WriteLine(string.Format("push {0}", Register.EAX));
			fw.WriteLine(string.Format("push {0}", Register.ECX));

			acceding.Lock.Release(Register.EAX);
			acceding.Lock.Release(Register.ECX);

			fw.WriteLine(string.Format("inc {0}", Register.ECX));
			fw.WriteLine(string.Format("shl {0}, {1}", Register.ECX, 2));
			NasmFunction.Malloc.Call(fw, reg, acceding, new NasmRegisterHolder(Register.ECX));


			fw.WriteLine(string.Format("pop {0}", Register.ECX));
			fw.WriteLine(string.Format("pop {0}", Register.EAX));
			acceding.Lock.Lock(Register.EAX);
			acceding.Lock.Lock(Register.ECX);

			fw.WriteLine(string.Format("mov {0}, {1}", Register.EDI, reg.Value));
			fw.WriteLine(string.Format("mov [{0}], {1}", Register.EDI, Register.ECX));
			fw.WriteLine(string.Format("add {0}, {1}", Register.EDI, 4));

			fw.WriteLine(string.Format("cmp {0}, 0", Register.ECX));
			fw.WriteLine(string.Format("je _{0}", zero.ToString("N")));			

			fw.WriteLine("cld");
			fw.WriteLine("rep stosd");

			fw.WriteLine(string.Format("mov {0}, {1}", Register.ECX, reg.Value));

			if (stackback) fw.WriteLine(string.Format("pop {0}", reg.Value));
			else
				acceding.Lock.Release(reg.Value);

			fw.DecrementIndentation();
			fw.WriteLine(string.Format("_{0}:", zero.ToString("N")));			
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
			fw.WriteLine(string.Format("cmp {0}, 0", Register.ECX));
			fw.WriteLine(string.Format("jge _{0}", ndoit.ToString("N")));
			EmitError(fw, acceding, bound, 2, "Array size must be non-negative");
			fw.WriteLine(string.Format("_{0}:", ndoit.ToString("N")));
			//</error checking>

			bool stackback = false;
			var reg = acceding.Lock.LockGPR(Register.EBX);
			if (reg == null)
			{
				stackback = true;
				reg = Register.EBX;
				fw.WriteLine(string.Format("push {0}", Register.EBX));
				
			}
			fw.WriteLine(string.Format(";push {0}",reg.Value));

			fw.WriteLine(string.Format("push {0}", Register.EAX));
			fw.WriteLine(string.Format("push {0}", Register.ECX));

			acceding.Lock.Release(Register.EAX);
			acceding.Lock.Release(Register.ECX);

			fw.WriteLine(string.Format("add {0}, 5", Register.ECX));
			NasmFunction.Malloc.Call(fw, reg, acceding, new NasmRegisterHolder(Register.ECX));

			fw.WriteLine(string.Format("pop {0}", Register.ECX));
			fw.WriteLine(string.Format("pop {0}", Register.EAX));
			acceding.Lock.Lock(Register.EAX);
			acceding.Lock.Lock(Register.ECX);

			fw.WriteLine(string.Format("mov {0}, {1}", Register.EDI, reg.Value));
			fw.WriteLine(string.Format("mov [{0}], {1}", Register.EDI, Register.ECX));
			fw.WriteLine(string.Format("add {0}, {1}", Register.EDI, 4));

			fw.WriteLine(string.Format("cmp {0}, 0", Register.ECX));
			fw.WriteLine(string.Format("je _{0}", zero.ToString("N")));

			fw.WriteLine("cld");
			fw.WriteLine("rep stosb");

			fw.WriteLine(string.Format("mov {0}, {1}", Register.ECX, reg.Value));

			if (stackback) fw.WriteLine(string.Format("pop {0}", reg.Value));
			else
				acceding.Lock.Release(reg.Value);

			fw.WriteLine(string.Format("_{0}:", zero.ToString("N")));
			fw.WriteLine(string.Format("xor {0}, {0}", Register.EAX.ByteVersion()));
			fw.WriteLine(string.Format("mov [{0}], {1}", Register.EDI, Register.EAX.ByteVersion()));
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
			Guid doit = bound.g.GNext();
			Guid ndoit = bound.g.GNext();
			fw.IncrementIndentation();

			//TODO: revisar chekeo de errores
			fw.WriteLine(string.Format("cmp {0}, 0", args[1]));
			fw.WriteLine(string.Format("jl _{0}", doit.ToString("N")));
			fw.WriteLine(string.Format("cmp {0}, [{1}]", args[1], args[0]));
			fw.WriteLine(string.Format("jge _{0}", doit.ToString("N")));

			fw.WriteLine(string.Format("jmp _{0}", ndoit.ToString("N")));
			fw.WriteLine(string.Format("_{0}:", doit.ToString("N")));
			EmitError(fw, acceding, bound, 1, "Index out of range");

			fw.WriteLine(string.Format("_{0}:", ndoit.ToString("N")));
			fw.WriteLine("inc " + args[1]);
			fw.WriteLine(string.Format("shl {0}, 2", args[1]));			
			fw.WriteLine(string.Format("add {0}, {1}", args[0], args[1]));
			fw.WriteLine(string.Format("mov {0}, [{0}]", args[0]));
			fw.DecrementIndentation();
		}

		static void MemberReadAccessByteString(FormatWriter fw, NasmEmitter bound, NasmEmitterScope acceding, Register[] args)
		{
			Guid doit = bound.g.GNext();
			Guid ndoit = bound.g.GNext();
			fw.IncrementIndentation();

			//TODO: revisar chekeo de errores
			fw.WriteLine(string.Format("cmp {0}, 0", args[1]));
			fw.WriteLine(string.Format("jl _{0}", doit.ToString("N")));
			fw.WriteLine(string.Format("cmp {0}, [{1}]", args[1], args[0]));
			fw.WriteLine(string.Format("jge _{0}", doit.ToString("N")));

			fw.WriteLine(string.Format("jmp _{0}", ndoit.ToString("N")));
			fw.WriteLine(string.Format("_{0}:", doit.ToString("N")));
			EmitError(fw, acceding, bound, 1, "Index out of range");

			fw.WriteLine(string.Format("_{0}:", ndoit.ToString("N")));
			fw.WriteLine(string.Format("add {0}, 4", args[1]));
			fw.WriteLine(string.Format("add {0}, {1}", args[0], args[1]));
			fw.WriteLine(string.Format("mov {0}, [{1}]", args[0].ByteVersion(), args[0]));
			fw.WriteLine(string.Format("movzx {0}, {1}", args[0], args[0].ByteVersion()));
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
			Guid doit = bound.g.GNext();
			Guid ndoit = bound.g.GNext();
			fw.IncrementIndentation();

			//TODO: revisar chekeo de errores
			fw.WriteLine(string.Format("cmp {0}, 0", args[1]));
			fw.WriteLine(string.Format("jl _{0}", doit.ToString("N")));
			fw.WriteLine(string.Format("cmp {0}, [{1}]", args[1], args[0]));
			fw.WriteLine(string.Format("jge _{0}", doit.ToString("N")));

			fw.WriteLine(string.Format("jmp _{0}", ndoit.ToString("N")));
			fw.WriteLine(string.Format("_{0}:", doit.ToString("N")));
			EmitError(fw, acceding, bound, 1, "Index out of range");

			fw.WriteLine(string.Format("_{0}:", ndoit.ToString("N")));
			fw.WriteLine("inc " + args[1]);
			fw.WriteLine(string.Format("shl {0}, 2", args[1]));
			fw.WriteLine(string.Format("add {0}, {1}", args[0], args[1]));
			fw.WriteLine(string.Format("mov [{0}], {1}", args[0], args[2]));
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
					default:
						break;
				}
			}
		}
		#endregion
	}

}
