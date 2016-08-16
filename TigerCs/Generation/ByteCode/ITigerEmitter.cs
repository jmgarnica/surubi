using System.Collections.Generic;
using System.IO;
using TigerCs.Generation.Semantic.Scopes;

namespace TigerCs.Generation.ByteCode
{
	public interface ITigerEmitter
	{
		TypeInfo VoidType { get; }
		TypeInfo Int { get; }
		TypeInfo String { get; }
		IDictionary<string, FunctionInfo> STD { get; }

		#region [Control]
		void Initialize(TextWriter writer, ErrorReport report);
		void End();

		void Comment(string comment);
		void BlankLine();

		void EmitError(int code);
		void EmitError(string message);
		#endregion

		#region [Bound]

		IHolder AddConstant(int value);
		IHolder AddConstant(string value);
		IHolder BoundVar(IType tigertype, string name = null);


		IFunction AheadFunctionDeclaration(string name, IType returntype, params IType[] args);

		[ScopeChanger(Reason = "Creates and enters in a function scope", ScopeName = "BoundedFuncion")]
		IFunction BoundFunction(string name, IType returntype, params IType[] args);

		[ScopeChanger(Reason = "Creates and enters in a function scope", ScopeName = "BoundedPreviousDeclaredFuncion")]
		void BoundFunction(IFunction aheadedfunction);


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

		void InstrRef(IHolder dest_nonconst, IHolder op1);
		IHolder InstrRef_TempBound(IHolder op1);

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
		void Call(FunctionInfo function, int paramcount, IHolder returnval = null);

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

		#region [Scope Handling]

		/// <summary>
		/// This is the common prefix of all lavels in the currentscope.
		/// </summary>
		string ScopeCommonLabel { get; }

		TigerScope RootScope { get; }
		TigerScope CurrentScope { get; }

		/// <summary>
		/// [IMPLEMENTATION_TIP] Add <paramref name="newscopelabel"/> to ScopeCommonLavel to make the common label of the new scope
		/// </summary>
		/// <param name="newscopelabel"></param>
		/// <param name="definetype">true if the new scope will define a new type</param>
		[ScopeChanger(Reason = "Creates and enters in a nested scope", ScopeName = "InnerScope")]
		void EnterNestedScope(string newscopelabel, bool definetype, string scopelabel = null);

		/// <summary>
		/// [IMPLEMENTATION_TIP] This should free any memory reserved in the scope that is about to leave.
		/// [IMPLEMENTATION_TIP] Remove the last part of ScopeCommonLavel, separators are plataform specific.
		/// [IMPLEMENTATION_TIP] Unused labels at the end of the scope will generate warnings
		/// [IMPLEMENTATION_TIP] Unseted label at the end of the scope generate critical errors
		/// </summary>
		[ScopeChanger(Reason = "Closes the current scope and returns to it's parent")]
		void LeaveScope();
		#endregion

		#region [GOTO]

		/// <summary>
		/// [IMPLEMENTATION_TIP] On labels collision returns the actual label;
		/// [IMPLEMENTATION_TIP] "END" label will mark the end of the scope automatically
		/// [IMPLEMENTATION_TIP] Duplicated label generate critical errors
		/// </summary>
		/// <param name="label"></param>
		/// <returns></returns>
		string SetLabelToNextInstruction(string label);

		/// <summary>
		/// [IMPLEMENTATION_TIP] jumping to unset label will not cause an error, the label will be buffered until it's assignment
		/// [IMPLEMENTATION_TIP] no jumps to outers scopes are allowed, you can get as far as END label
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
