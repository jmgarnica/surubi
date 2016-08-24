using System;

namespace TigerCs.Generation.ByteCode
{
	public interface IByteCodeMachine<T, F, H>
		where T : class, IType<T, F>
		where F : class, IFunction<T, F>
		where H : class, IHolder
	{
		void InitializeCodeGeneration(ErrorReport report);
		void End();

		#region [Control]
		void Comment(string comment);
		void BlankLine();

		void EmitError(int code);
		void EmitError(string message);
		#endregion

		#region [Bound]

		H AddConstant(int value);
		H AddConstant(string value);
		H BoundVar(T tigertype, string name = null);

		[ScopeChanger(Reason = "Creates and enters in a function scope", ScopeName = "BoundedFuncion")]
		F BoundFunction(string name, T returntype, params Tuple<string,T>[] args);

		[ScopeChanger(Reason = "Creates and enters in a function scope", ScopeName = "BoundedPreviousDeclaredFuncion")]
		void BoundFunction(F aheadedfunction);

		T BoundRecordType(string name, params Tuple<string, T>[] members);
		void BoundRecordType(T aheadedtype, params Tuple<string, T>[] members);

		T BoundArrayType(string name, T underlayingtype);
		void BoundArrayType(T aheadedtype, T underlayingtype);

		F AheadFunctionDeclaration(string name, T returntype, params Tuple<string, T>[] args);
		T AheadTypeDeclaration(string name);

		#region [Bound STD]
		//any fail to bound a std function will generate a critical error

		T STDBoundType(string name);

		F STDBoundFunction(string name);

		H STDBoundConst(string name);

		#endregion

		#endregion

		#region [General Instructions]
		void InstrAssing(H dest_nonconst, H value);

		void InstrAdd(H dest_nonconst, H op1, H op2);
		H InstrAdd_TempBound(H op1, H op2);

		void InstrSub(H dest_nonconst, H op1, H op2);
		H InstrSub_TempBound(H op1, H op2);

		void InstrMult(H dest_nonconst, H op1, H op2);
		H InstrMult_TempBound(H op1, H op2);

		void InstrDiv(H dest_nonconst, H op1, H op2);
		H InstrDiv_TempBound(H op1, H op2);

		void InstrInverse(H dest_nonconst, H op1);
		H InstrInverse_TempBound(H op1);

		void InstrRefEq(H dest_nonconst, H op1, H op2);
		H InstrRefEq_TempBound( H op1, H op2);

		#endregion

		#region [Call]
		/// <summary>
		/// Push a parameter in a stack for latter use whith call. 
		/// [IMPLEMENTATION_TIP] Ending a program with a non-empty stack will result in error.
		/// </summary>
		/// <param name="holder"></param>
		void Param(H holder);
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
		void Call(F function, int paramcount, H returnval = null);

		/// <summary>
		/// All function path must end with one form of return. An error will be generated if not.
		/// </summary>
		/// <param name="value"></param>
		void Ret(H value);

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
		void GotoIfZero(string label, H op1);

		/// <summary>
		/// [IMPLEMENTATION_TIP] jumping to unset label will not cause an error, the label will be buffered until it's assignment
		/// [IMPLEMENTATION_TIP] no jumps to outers scopes are allowed, you can get as far as END label
		/// </summary>
		/// <param name="label"></param>
		/// <param name="op1"></param>
		void GotoIfNotZero(string label, H op1);

		#endregion

		#region [Scope Handling]

		/// <summary>
		/// [IMPLEMENTATION_TIP] Add <paramref name="newscopelabel"/> to ScopeCommonLavel to make the common label of the new scope
		/// </summary>
		/// <param name="newscopelabel"></param>
		/// <param name="definetype">true if the new scope will define a new type</param>
		[ScopeChanger(Reason = "Creates and enters in a nested scope", ScopeName = "InnerScope")]
		void EnterNestedScope(string newscopelabel, bool definetype, string scopelabel = null);

		/// <summary>
		/// [IMPLEMENTATION_TIP] This should free any memory reserved in the scope that is about to leave.
		/// [IMPLEMENTATION_TIP] Remove the last part of ScopeCommonLavel, separators are TigerScope.ScopeNameSeparator
		/// [IMPLEMENTATION_TIP] Unused labels at the end of the scope will generate warnings
		/// [IMPLEMENTATION_TIP] Unseted label at the end of the scope generate critical errors
		/// </summary>
		[ScopeChanger(Reason = "Closes the current scope and returns to it's parent")]
		void LeaveScope();

		#endregion
	}
}
