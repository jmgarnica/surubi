using System.IO;
using TigerCs.Generation.Semantic.Scopes;

namespace TigerCs.Generation.ByteCode
{
	public interface ITigerEmitter : ISemanticStandar
	{
		void InitializeCodeGeneration(IWriter writer, ErrorReport report);
		void End();

		#region [Control]
		void Comment(string comment);
		void BlankLine();

		void EmitError(int code);
		void EmitError(string message);
		#endregion

		#region [Bound]

		IHolder AddConstant(int value);
		IHolder AddConstant(string value);
		IHolder BoundVar(IType tigertype, string name = null);

		[ScopeChanger(Reason = "Creates and enters in a function scope", ScopeName = "BoundedFuncion")]
		IFunction BoundFunction(string name, IType returntype, params IType[] args);

		[ScopeChanger(Reason = "Creates and enters in a function scope", ScopeName = "BoundedPreviousDeclaredFuncion")]
		void BoundFunction(IFunction aheadedfunction);

		IFunction AheadFunctionDeclaration(string name, IType returntype, params IType[] args);
		IType AheadTypeDeclaration(string name);

		#endregion

		#region [General Instructions]
		void InstrAssing(IHolder dest_nonconst, IHolder value);

		void InstrAdd(IHolder dest_nonconst, IHolder op1, IHolder op2);
		IHolder InstrAdd_TempBound(IHolder op1, IHolder op2);

		void InstrSub(IHolder dest_nonconst, IHolder op1, IHolder op2);
		IHolder InstrSub_TempBound(IHolder op1, IHolder op2);

		void InstrMult(IHolder dest_nonconst, IHolder op1, IHolder op2);
		IHolder InstrMult_TempBound(IHolder op1, IHolder op2);

		void InstrDiv(IHolder dest_nonconst, IHolder op1, IHolder op2);
		IHolder InstrDiv_TempBound(IHolder op1, IHolder op2);

		void InstrInverse(IHolder dest_nonconst, IHolder op1);
		IHolder InstrInverse_TempBound(IHolder op1);

		void InstrRefEq(IHolder dest_nonconst, IHolder op1, IHolder op2);
		IHolder InstrRefEq_TempBound( IHolder op1, IHolder op2);

		#endregion

		#region [Call]
		/// <summary>
		/// Push a parameter in a stack for latter use whith call. 
		/// [IMPLEMENTATION_TIP] Ending a program with a non-empty stack will result in error.
		/// </summary>
		/// <param name="holder"></param>
		void Param(IHolder holder);
		//TODO: mejorar param... la pila hace falta?
		/// <summary>
		/// Enters in a function with the <paramref name="paramcount"/> parameters on the top of the stack as it's parameters.
		/// [IMPLEMENTATION_TIP] The parameters are deleted from the stack.
		/// </summary>
		/// <param name="function"></param>
		/// <param name="paramcount"></param>
		/// <param name="returnval">
		/// if diferent of null the return value of the function will be placed here
		/// </param>
		void Call(IFunction function, int paramcount, IHolder returnval = null);

		/// <summary>
		/// All function path must end with one form of return. An error will be generated if not.
		/// </summary>
		/// <param name="value"></param>
		void Ret(IHolder value);

		/// <summary>
		/// All function path must end with one form of return. An error will be generated if not.
		/// </summary>
		void Ret();
		#endregion

		#region [GOTO & Labeling]

		/// <summary>
		/// This is the common prefix of all labels in the currentscope.
		/// </summary>
		string ScopeCommonLabel { get; }

		/// <summary>
		/// [IMPLEMENTATION_TIP] On labels collision returns the actual label;
		/// [IMPLEMENTATION_TIP] "END" label will mark the end of the scope automatically
		/// [IMPLEMENTATION_TIP] if the label for the next instruction is already setted will return the setted label
		/// </summary>
		/// <param name="label"></param>
		/// <returns></returns>
		string SetLabelToNextInstruction(string label);

		/// <summary>
		/// [IMPLEMENTATION_TIP] jumping to unset label will not cause an error(unless the code are been interpretted), the label will be buffered until it's assignment
		/// [IMPLEMENTATION_TIP] no jumps to outer or inner scopes are allowed, you can get as far as the END label
		/// [IMPLEMENTATION_TIP] labels are relative to the current scope and can't contain the scope separator(this will generate an error)
		/// </summary>
		/// <param name="label"></param>
		void Goto(string label);

		/// <summary>
		/// [IMPLEMENTATION_TIP] jumping to unset label will not cause an error, the label will be buffered until it's assignment
		/// [IMPLEMENTATION_TIP] no jumps to outers scopes are allowed, you can get as far as END label
		/// </summary>
		/// <param name="label"></param>
		/// <param name="op1"></param>
		void GotoIfZero(string label, IHolder op1);

		/// <summary>
		/// [IMPLEMENTATION_TIP] jumping to unset label will not cause an error, the label will be buffered until it's assignment
		/// [IMPLEMENTATION_TIP] no jumps to outers scopes are allowed, you can get as far as END label
		/// </summary>
		/// <param name="label"></param>
		/// <param name="op1"></param>
		void GotoIfNotZero(string label, IHolder op1);

		#endregion

	}
}
//TODO: type instantiation
