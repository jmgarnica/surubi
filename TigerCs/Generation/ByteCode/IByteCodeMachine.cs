using System;
using TigerCs.CompilationServices;

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

		void EmitError(int code, string message = null);

		int SourceLine { set; }
		int SourceColumn { set; }
		//debug
		//int ByteCodeLine { get; }
		//int ByteCodeColumn { get; }

		/// <summary>
		/// invalidate the holder making it eligible for temp holders and optimizations
		/// [IMPLEMENTATION_TIP] Should not be used with the same meaning this point on.
		/// [IMPLEMENTATION_TIP] All off-scope holders become invalid, there is no point in doing this at the end of a scope
		/// </summary>
		/// <param name="holder"></param>
		void Release(H holder);
		#endregion

		#region [Bind]

		#region [Holders]
		H AddConstant(int value);
		H AddConstant(string value);
		H BindVar(T tigertype = null, H defaultvalue = null, string name = null, bool global = false);

		H StaticMemberAcces(T tigertype, H op1, int index);
		#endregion

		#region [Functions]
		[ScopeChanger(Reason = "Creates and enters in the primary scope of the program, this has no parent scope after closing it no fouther instructions can be emitted", ScopeName = "Main")]
		F EntryPoint(bool returns = false, bool stringparams = false);

		F DeclareFunction(string name, T returntype, Tuple<string, T>[] args, bool global = false);

		[ScopeChanger(Reason = "Creates and enters in a function scope", ScopeName = "Funcion_<name>")]
		F BindFunction(string name, T returntype, Tuple<string, T>[] args, bool global = false);
		[ScopeChanger(Reason = "Creates and enters in a function scope", ScopeName = "AheadedFuncion_<name>")]
		void BindFunction(F aheadedfunction);
		#endregion

		#region [Types]
		T DeclareType(string name);

		T BindRecordType(string name, Tuple<string, T>[] members, bool global = false);
		void BindRecordType(T aheadedtype, Tuple<string, T>[] members, bool global = false);

		T BindArrayType(string name, T underlayingtype);
		void BindArrayType(T aheadedtype, T underlayingtype);
		#endregion

		#region [STD]

		bool TryBindSTDType(string name, out T type);

		bool TryBindSTDFunction(string name, out F function);

		bool TryBindSTDConst(string name, out H constant);

		#endregion

		#endregion

		#region [General Instructions]

		/// <summary>
		/// </summary>
		/// <param name="dest_nonconst"></param>
		/// <param name="value"></param>
		void InstrAssing(H dest_nonconst, H value);

		void InstrAdd(H dest_nonconst, H op1, H op2);

		void InstrSub(H dest_nonconst, H op1, H op2);

		void InstrMult(H dest_nonconst, H op1, H op2);

		void InstrDiv(H dest_nonconst, H op1, H op2);

		void InstrInverse(H dest_nonconst, H op1);

		void InstrRefEq(H dest_nonconst, H op1, H op2);

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
		void InstrSize(H array, H size);

		#endregion

		#region [Call]

		/// <summary>
		/// Retuns the holder that will contain the value of the parameter inside the function
		/// [IMPLEMENTATION_TIP] zero base positions
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		H GetParam(int position);

		/// <summary>
		/// Enters in a function.
		/// </summary>
		/// <param name="function"></param>
		/// <param name="args"></param>
		/// <param name="returnval">
		/// if different of null the return value of the function will be placed there
		/// </param>
		void Call(F function, H[] args, H returnval = null);

		/// <summary>
		/// All function path must end with one form of return. An error will be generated if not.
		/// </summary>
		/// <param name="value"></param>
		void Ret(H value = null);

		#endregion

		#region [GOTO & Labeling]

		/// <summary>
		/// Scope dependent label, before enter the scope
		/// </summary>
		Guid BeforeEnterScope { get; }

		/// <summary>
		/// Scope dependent label, first instruction of the current scope
		/// </summary>
		Guid BiginScope { get; }

		/// <summary>
		/// Scope dependent label, before exiting the scope
		/// </summary>
		Guid EndScope { get; }

		/// <summary>
		/// Scope dependent label, after exiting the scope
		/// </summary>
		Guid AfterEndScope { get; }

		/// <summary>
		/// Returns the next instruction label if there is one asingned, Guid.empty other way, if not empty setting
		/// a new label only will have effect on the commentary and applying a reserved label will result in
		/// an InvalidOperationException
		/// </summary>
		Guid NextInstructionLabel { get; }

		/// <summary>
		/// [IMPLEMENTATION_TIP] On labels collision returns the actual label;
		/// [IMPLEMENTATION_TIP] if the label for the next instruction is already setted will return the setted label
		/// </summary>
		/// <param name="label"></param>
		/// <returns></returns>
		Guid SetLabelToNextInstruction(string label);

		Guid ReserveInstructionLabel(string label);
		void ApplyReservedLabel(Guid reservedlabel);

		/// <summary>
		/// [IMPLEMENTATION_TIP] jumping to unset label will not cause an error if the label is reserved
		/// Jumps are only legal inside the current scope or parent scope, and not to an inner scope of these.
		/// This can not cross over functions borders.
		/// </summary>
		/// <param name="label"></param>
		void Goto(Guid label);

		/// <summary>
		/// [IMPLEMENTATION_TIP] jumping to unset label will not cause an error if the label is reserved
		/// No jumps to outers or inner scopes are allowed.
		/// BeforeEnterScope and AfterEndScope labels are not part of the current scope.
		/// This can not cross over functions borders.
		/// </summary>
		void GotoIfZero(Guid label, H int_op);

		/// <summary>
		/// [IMPLEMENTATION_TIP] jumping to unset label will not cause an error if the label is reserved
		/// No jumps to outers or inner scopes are allowed.
		/// BeforeEnterScope and AfterEndScope labels are not part of the current scope.
		/// This can not cross over functions borders.
		/// </summary>
		void GotoIfNotZero(Guid label, H int_op);

		/// <summary>
		/// [IMPLEMENTATION_TIP] jumping to unset label will not cause an error if the label is reserved
		/// No jumps to outers or inner scopes are allowed.
		/// BeforeEnterScope and AfterEndScope labels are not part of the current scope.
		/// This can not cross over functions borders.
		/// [IMPLEMENTATION_TIP] int_op >= 0
		/// </summary>
		void GotoIfNotNegative(Guid label, H int_op);

		/// <summary>
		/// [IMPLEMENTATION_TIP] jumping to unset label will not cause an error if the label is reserved
		/// No jumps to outers or inner scopes are allowed.
		/// BeforeEnterScope and AfterEndScope labels are not part of the current scope.
		/// This can not cross over functions borders.
		/// [IMPLEMENTATION_TIP] int_op less than 0
		/// </summary>
		void GotoIfNegative(Guid label, H int_op);

		#endregion

		#region [Scope Handling]

		/// <summary>
		/// </summary>
		/// <param name="definetype">true if the new scope will contains type definitions</param>
		/// <param name="namehint">
		/// [IMPLEMENTATION_TIP] adds a comment befor enter the scope on the compiled code
		/// </param>
		[ScopeChanger(Reason = "Creates and enters in a nested scope", ScopeName = "InnerScope_<scopelabel>")]
		void EnterNestedScope(bool definetype = false, string namehint = null);

		/// <summary>
		/// [IMPLEMENTATION_TIP] This should free any memory reserved in the scope that is about to leave.
		/// [IMPLEMENTATION_TIP] Unused labels at the end of the scope will generate warnings
		/// [IMPLEMENTATION_TIP] Unset label at the end of the scope generate critical errors
		/// </summary>
		[ScopeChanger(Reason = "Closes the current scope and returns to it's parent")]
		void LeaveScope();

		#endregion
	}

	public static class IByteCodeMachineExtensions

	{
		public static H InstrAdd_TempBound<T, F, H>(this IByteCodeMachine<T, F, H> machine, H op1, H op2)
			where T : class, IType<T, F>
			where F : class, IFunction<T, F>
			where H : class, IHolder
		{
			H tmp = machine.BindVar();
			machine.InstrAdd(tmp, op1, op2);
			return tmp;
		}

		public static H InstrSub_TempBound<T, F, H>(this IByteCodeMachine<T, F, H> machine, H op1, H op2)
			where T : class, IType<T, F>
			where F : class, IFunction<T, F>
			where H : class, IHolder
		{
			H tmp = machine.BindVar();
			machine.InstrSub(tmp, op1, op2);
			return tmp;
		}

		public static H InstrMult_TempBound<T, F, H>(this IByteCodeMachine<T, F, H> machine, H op1, H op2)
			where T : class, IType<T, F>
			where F : class, IFunction<T, F>
			where H : class, IHolder
		{
			H tmp = machine.BindVar();
			machine.InstrMult(tmp, op1, op2);
			return tmp;
		}

		public static H InstrDiv_TempBound<T, F, H>(this IByteCodeMachine<T, F, H> machine, H op1, H op2)
			where T : class, IType<T, F>
			where F : class, IFunction<T, F>
			where H : class, IHolder
		{
			H tmp = machine.BindVar();
			machine.InstrDiv(tmp, op1, op2);
			return tmp;
		}

		public static H InstrInverse_TempBound<T, F, H>(this IByteCodeMachine<T, F, H> machine, H op1)
			where T : class, IType<T, F>
			where F : class, IFunction<T, F>
			where H : class, IHolder
		{
			H tmp = machine.BindVar();
			machine.InstrInverse(tmp, op1);
			return tmp;
		}

		public static H InstrRefEq_TempBound<T, F, H>(this IByteCodeMachine<T, F, H> machine, H op1, H op2)
			where T : class, IType<T, F>
			where F : class, IFunction<T, F>
			where H : class, IHolder
		{
			H tmp = machine.BindVar();
			machine.InstrRefEq(tmp, op1, op2);
			return tmp;
		}

		public static H InstrSize_TempBound<T, F, H>(this IByteCodeMachine<T, F, H> machine, H array)
			where T : class, IType<T, F>
			where F : class, IFunction<T, F>
			where H : class, IHolder
		{
			H tmp = machine.BindVar();
			machine.InstrSize(array, tmp);
			return tmp;
		}
    }
}
