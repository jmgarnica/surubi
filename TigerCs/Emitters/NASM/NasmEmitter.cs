using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Emitters.NASM
{
	public class NasmEmitter : BCMBase<NasmType, NasmFunction, NasmHolder>
	{
		FormatWriter fw;
		ErrorReport r;
		StreamWriter t;
		NasmEmitterScope currentscope;
		public const string StringConstName = "stringconst";
		public const string IntConstName = "intconst";
		public const string ErrorFlag = "error";
		Dictionary<int, NasmHolder> IntConst;
		Dictionary<string, NasmHolder> StringConst;
		int StringConstEnd;

		public NasmEmitter(StreamWriter target)
		{
			t = target;
		}

		public override void InitializeCodeGeneration(ErrorReport report)
		{
			fw = new FormatWriter();
			r = report;
		}
		public override void End()
		{
			fw.Flush(t);
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
			var curr = currentscope;
			do
			{
				curr.WirteCloseCode(fw);
				if (curr.FunctionScope) break;
				curr = curr.Parent;
			} while (curr != null);

			var hint = AddConstant(code);

			if (message != null)
			{
				var ms = AddConstant(message);
				//fw.WriteLine(string.Format("mov edx, [{0} + {1}]", StringConstName, ms.ConstantIndex));
				fw.WriteLine("PRINT_STRING [edx]");
			}

			//fw.WriteLine(string.Format("mov eax, [{0} + {1}]", IntConstName, hint.ConstantIndex));
			fw.WriteLine(string.Format("mov {0}, dword 1", ErrorFlag));
			fw.WriteLine("ret");
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
			NasmHolder ret;
			if (IntConst.TryGetValue(value, out ret)) return ret;

			//ret = new NasmHolder { ConstantIndex = IntConst.Count * 4 };
			//IntConst.Add()
			throw new NotImplementedException();
		}
		public override NasmHolder AddConstant(string value){ throw new NotImplementedException(); }
		public override NasmHolder BindVar(NasmType tigertype, string name = null, bool global = false){ throw new NotImplementedException(); }
		#endregion

		#region [Functions]
		[ScopeChanger(Reason = "Creates and enters in the primary scope of the program, this has no parent scope after closing it no fouther instructions can be emitted", ScopeName = "Main")]
		public override NasmFunction EntryPoint(bool returns = false, bool stringparams = false){ throw new NotImplementedException(); }

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

		public override bool TryBindSTDType(string name, out NasmType type){ throw new NotImplementedException(); }

		public override bool TryBindSTDFunction(string name, out NasmFunction function){ throw new NotImplementedException(); }

		public override bool TryBindSTDConst(string name, out NasmHolder constant){ throw new NotImplementedException(); }

		#endregion

		#endregion

		#region [General Instructions]
		public override void InstrAssing(NasmHolder dest_nonconst, NasmHolder value){ throw new NotImplementedException(); }

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
		/// <param name="array">
		/// 
		/// </param>
		/// <param name="size">
		/// a holder to write the result
		/// </param>
		/// <returns></returns>
		public override void InstrSize(NasmHolder array, NasmHolder size){ throw new NotImplementedException(); }

		#endregion

		#region [Call]
		/// <summary>
		/// Push a parameter in a stack for latter use whith call. 
		/// [IMPLEMENTATION_TIP] Ending a program with a non-empty stack will result in error.
		/// </summary>
		/// <param name="holder"></param>
		public override void SetParam(NasmHolder holder){ throw new NotImplementedException(); }

		/// <summary>
		/// Retuns the holder that will contain the value of the parameter inside the function
		/// [IMPLEMENTATION_TIP] zero base positions
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		public override NasmHolder GetParam(int position){ throw new NotImplementedException(); }

		/// <summary>
		/// Enters in a function with the <paramref name="paramcount"/> parameters on the top of the stack as it's parameters.
		/// [IMPLEMENTATION_TIP] The parameters are deleted from the stack.
		/// </summary>
		/// <param name="function"></param>
		/// <param name="paramcount"></param>
		/// <param name="returnval">
		/// if diferent of null the return value of the function will be placed here
		/// </param>
		public override void Call(NasmFunction function, int paramcount, NasmHolder returnval = null){ throw new NotImplementedException(); }

		/// <summary>
		/// All function path must end with one form of return. An error will be generated if not.
		/// </summary>
		/// <param name="value"></param>
		public override void Ret(NasmHolder value = null){ throw new NotImplementedException(); }

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
		public override Guid SetLabelToNextInstruction(string label){ throw new NotImplementedException(); }

		public override Guid ReserveInstructionLabel(string label){ throw new NotImplementedException(); }
		public override void ApplyReservedLabel(Guid reservedlabel){ throw new NotImplementedException(); }

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
		public override void LeaveScope(){ throw new NotImplementedException(); }

		#endregion


		#region [Helpers]

		public static void CodeCall(FormatWriter fw, NasmFunction f, Register? result, NasmEmitterScope currentscope, params NasmHolder[] args)
		{
			var lockreglist = currentscope.Lock.Locked();
			fw.WriteLine(string.Format(";before calling {0}", f.Name));

			foreach (var r in lockreglist)
			{
				if (!r.GeneralPurposeRegister()) continue;
				fw.WriteLine("push " + r);
				currentscope.Lock.Release(r);
			}

			fw.WriteLine(string.Format("; calling {0}", f.Name));
			fw.WriteLine("push EBP");

			currentscope.Lock.Lock(Register.EAX);

			foreach (var arg in args)
			{
				arg.PutValueInRegister(Register.EAX, fw, currentscope);
				fw.WriteLine("push " + Register.EAX);
			}
		}

		#endregion
	}

}
