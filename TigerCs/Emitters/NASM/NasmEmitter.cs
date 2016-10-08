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
			Externs = new HashSet<string>();
			std = new Dictionary<string, NasmFunction>();
			PrintS = new NasmCFunction("_cprintS", false, this);
			std["prints"] = PrintS;
			fw = new FormatWriter();
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
			NasmFunction.Malloc = new NasmCFunction("_malloc", true, this);
			NasmFunction.Free = new NasmCFunction("_free", true, this);

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
			var ret = AddConstant(code);
			if (message != null)
			{
				var ms = AddConstant(message);
				PrintS.Call(fw, null, CurrentScope, ms);
			}

			var curr = CurrentScope;
			while(curr != null && curr.ScopeType == NasmScopeType.Nested)
			{
				curr.WirteCloseCode(fw, false, true);				
				curr = curr.Parent;
			}

			if (curr != null) curr.WirteCloseCode(fw, false, true, ret);
		}

		/// <summary>
		/// invalidate the holder making it eligible for temp holders and optimizations
		/// [IMPLEMENTATION_TIP] Should not be used with the same meaning this point on.
		/// [IMPLEMENTATION_TIP] All off-scope holders become invalid, there is no point in doing this at the end of a scope
		/// </summary>
		/// <param name="holder"></param>
		public override void Release(NasmHolder holder){ throw new NotImplementedException(); }
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
		public override NasmHolder BindVar(NasmType type, string name = null, bool global = false)
		{
			if (name != null) fw.WriteLine(string.Format("; {0}<EBP - {1}>", name, (CurrentScope.VarsCount + 1) * 4));
			NasmHolder v = new NasmHolder(CurrentScope, CurrentScope.VarsCount);
			CurrentScope.VarsCount++;
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
			return new NasmReference(op1, index, CurrentScope);
		}
		#endregion

		#region [Functions]
		[ScopeChanger(Reason = "Creates and enters in the primary scope of the program, this has no parent scope after closing it no fouther instructions can be emitted", ScopeName = "Main")]
		public override NasmFunction EntryPoint(bool returns = false, bool stringparams = false)
		{
			CurrentScope = new NasmEmitterScope(null, g.GNext(), g.GNext(), g.GNext(), NasmScopeType.CFunction, 2);
			
			fw.WriteLine(";Main");
			fw.WriteLine("CMAIN:");
			fw.WriteLine(string.Format("_{0}:", CurrentScope.BeforeEnterScope.ToString("N")));
			fw.IncrementIndentation();
			CurrentScope.WriteEnteringCode(fw);
			fw.WriteLine(string.Format("_{0}:", CurrentScope.BiginScope.ToString("N")));

			return new NasmCFunction(CurrentScope.BiginScope.ToString(), false, this);
		}

		public override NasmFunction DeclareFunction(string name, NasmType returntype, Tuple<string, NasmType>[] args, bool global = false){ throw new NotImplementedException(); }

		[ScopeChanger(Reason = "Creates and enters in a function scope", ScopeName = "Funcion_<name>")]
		public override NasmFunction BindFunction(string name, NasmType returntype, Tuple<string, NasmType>[] args, bool global = false){ throw new NotImplementedException(); }
		[ScopeChanger(Reason = "Creates and enters in a function scope", ScopeName = "AheadedFuncion_<name>")]
		public override void BindFunction(NasmFunction aheadedfunction){ throw new NotImplementedException(); }
		#endregion

		#region [Types]
		public override NasmType DeclareType(string name){ throw new NotImplementedException(); }

		public override NasmType BindRecordType(string name, Tuple<string, NasmType>[] members, bool global = false){ throw new NotImplementedException(); }
		public override void BindRecordType(NasmType aheadedtype, Tuple<string, NasmType>[] members, bool global = false){ throw new NotImplementedException(); }

		public override NasmType BindArrayType(string name, NasmType underlayingtype){ throw new NotImplementedException(); }
		public override void BindArrayType(NasmType aheadedtype, NasmType underlayingtype){ throw new NotImplementedException(); }
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
			switch (name.ToLower())
			{
				case "prints":
					function = std["prints"];
					return true;

				case "printi":
					function = std["printi"] = new NasmCFunction("_cprinti", false, this);
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
			return new NasmHolder(CurrentScope, 4 * (position + 3));
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
			bool stackbacked = false;
			if (returnval != null && reg == null)
			{
				stackbacked = true;
				fw.WriteLine("push " + Register.EDX);
				reg = Register.EDX;
			}

			function.Call(fw, reg, CurrentScope, args);

			if (reg != null)
			{
				returnval.StackBackValue(reg.Value, fw, CurrentScope);
				if (stackbacked) fw.WriteLine("pop " + reg.Value);
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
		public override void EnterNestedScope(bool definetype = false, string namehint = null){ throw new NotImplementedException(); }

		/// <summary>
		/// [IMPLEMENTATION_TIP] This should free any memory reserved in the scope that is about to leave.
		/// [IMPLEMENTATION_TIP] Unused labels at the end of the scope will generate warnings
		/// [IMPLEMENTATION_TIP] Unset label at the end of the scope generate critical errors
		/// </summary>
		[ScopeChanger(Reason = "Closes the current scope and returns to it's parent")]
		public override void LeaveScope()
		{
			if (SetLabel())
			{
				if (CurrentScope.ScopeType == NasmScopeType.Nested)
					CurrentScope.WirteCloseCode(fw, true, false);
				else fw.WriteLine("nop");
			}
			fw.DecrementIndentation();
			fw.WriteLine(string.Format("_{0}:", CurrentScope.BiginScope.ToString("N")));
			CurrentScope = CurrentScope.Parent;
		}

		#endregion

		#region [Helpers]

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
		public void CatchAndRethrow()
		{
			fw.WriteLine(string.Format("cmp {0}, dword {1}", Register.ECX, ErrorCode));
			var ndoit = ReserveInstructionLabel("[no error]");
			fw.WriteLine(string.Format("jne _{0}", ndoit.ToString("N")));

			var curr = CurrentScope;
			while (curr != null && curr.ScopeType == NasmScopeType.Nested)
			{
				curr.WirteCloseCode(fw, false, true);
				curr = curr.Parent;
			}

			if (curr != null) curr.WirteCloseCode(fw, false, true);

			ApplyReservedLabel(ndoit);
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

		void AddPrintS(FormatWriter fw)
		{
			fw.WriteLine("_cprintS:");
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

		void AddPrintI(FormatWriter fw)
		{
			fw.WriteLine("_cprintI:");
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
