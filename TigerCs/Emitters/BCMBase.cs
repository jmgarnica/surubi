using System;
using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Emitters
{
	public abstract class BCMBase<T, F, H, S> : IByteCodeMachine<T,F,H>
		where T : class, IType<T, F>
		where F : class, IFunction<T, F>
		where H : class, IHolder
		where S : EmitterScope<S>
	{
		protected GuidGenerator g { get; private set; }
		protected string nexlabelcomment;
		protected S CurrentScope;

		public abstract void InitializeCodeGeneration(ErrorReport report);
		public abstract void End();

		protected BCMBase()
		{
			g = new GuidGenerator();
		}

		#region [Control]
		public abstract void Comment(string comment);
		public abstract void BlankLine();

		public abstract void EmitError(int code, string message = null);

		/// <summary>
		/// invalidate the holder making it eligible for temp holders and optimizations
		/// [IMPLEMENTATION_TIP] Should not be used with the same meaning this point on.
		/// [IMPLEMENTATION_TIP] All off-scope holders become invalid, there is no point in doing this at the end of a scope
		/// </summary>
		/// <param name="holder"></param>
		public abstract void Release(H holder);
		#endregion

		#region [Bind]

		#region [Holders]
		public abstract H AddConstant(int value);
		public abstract H AddConstant(string value);
		public abstract H BindVar(T tigertype, string name = null, bool global = false);
		public abstract H StaticMemberAcces(T tigertype, H op1, int index);
		#endregion

		#region [Functions]
		[ScopeChanger(Reason = "Creates and enters in the primary scope of the program, this has no parent scope after closing it no fouther instructions can be emitted", ScopeName = "Main")]
		public abstract F EntryPoint(bool returns = false, bool stringparams = false);

		public abstract F DeclareFunction(string name, T returntype, Tuple<string, T>[] args, bool global = false);

		[ScopeChanger(Reason = "Creates and enters in a function scope", ScopeName = "Funcion_<name>")]
		public abstract F BindFunction(string name, T returntype, Tuple<string, T>[] args, bool global = false);
		[ScopeChanger(Reason = "Creates and enters in a function scope", ScopeName = "AheadedFuncion_<name>")]
		public abstract void BindFunction(F aheadedfunction);
		#endregion

		#region [Types]
		public abstract T DeclareType(string name);

		public abstract T BindRecordType(string name, Tuple<string, T>[] members, bool global = false);
		public abstract void BindRecordType(T aheadedtype, Tuple<string, T>[] members, bool global = false);

		public abstract T BindArrayType(string name, T underlayingtype);
		public abstract void BindArrayType(T aheadedtype, T underlayingtype);
		#endregion

		#region [STD]

		public abstract bool TryBindSTDType(string name, out T type);

		public abstract bool TryBindSTDFunction(string name, out F function);

		public abstract bool TryBindSTDConst(string name, out H constant);

		#endregion

		#endregion

		#region [General Instructions]
		/// <summary>
		/// </summary>
		/// <param name="dest_nonconst"></param>
		/// <param name="value"></param>
		/// <param name="dest_as_pointer"></param>
		public abstract void InstrAssing(H dest_nonconst, H value);

		public abstract void InstrAdd(H dest_nonconst, H op1, H op2);
		public abstract H InstrAdd_TempBound(H op1, H op2);

		public abstract void InstrSub(H dest_nonconst, H op1, H op2);
		public abstract H InstrSub_TempBound(H op1, H op2);

		public abstract void InstrMult(H dest_nonconst, H op1, H op2);
		public abstract H InstrMult_TempBound(H op1, H op2);

		public abstract void InstrDiv(H dest_nonconst, H op1, H op2);
		public abstract H InstrDiv_TempBound(H op1, H op2);

		public abstract void InstrInverse(H dest_nonconst, H op1);
		public abstract H InstrInverse_TempBound(H op1);

		public abstract void InstrRefEq(H dest_nonconst, H op1, H op2);
		public abstract H InstrRefEq_TempBound(H op1, H op2);

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
		public abstract void InstrSize(H array, H size);
		#endregion

		#region [Call]

		/// <summary>
		/// Retuns the holder that will contain the value of the parameter inside the function
		/// [IMPLEMENTATION_TIP] zero base positions
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		public abstract H GetParam(int position);

		/// <summary>
		/// Enters in a function.
		/// </summary>
		/// <param name="function"></param>
		/// <param name="args"></param>
		/// <param name="returnval">
		/// if diferent of null the return value of the function will be placed there
		/// </param>
		public abstract void Call(F function, H[] args, H returnval = null);

		/// <summary>
		/// All function path must end with one form of return. An error will be generated if not.
		/// </summary>
		/// <param name="value"></param>
		public abstract void Ret(H value = null);

		#endregion

		#region [GOTO & Labeling]

		/// <summary>
		/// Scope dependent label, before enter the scope
		/// </summary>
		public Guid BeforeEnterScope { get { return CurrentScope.BeforeEnterScope; } }

		/// <summary>
		/// Scope dependent label, first instruction of the current scope
		/// </summary>
		public Guid BiginScope { get { return CurrentScope.BiginScope; } }

		/// <summary>
		/// Scope dependent label, before exiting the scope
		/// </summary>
		public Guid EndScope { get { return CurrentScope.EndScope; } }

		public Guid AfterEndScope { get { return CurrentScope.AfterEndScope; } }

		/// <summary>
		/// Returns the next instruction label if there is one asingned, Guid.empty other way, if not empty setting
		/// a new label only will have effect on the commentary and applying a reserved label will result in
		/// an InvalidOperationException
		/// </summary>
		public Guid NextInstructionLabel { get; protected set; }

		/// <summary>
		/// [IMPLEMENTATION_TIP] On labels collision returns the actual label;
		/// [IMPLEMENTATION_TIP] if the label for the next instruction is already setted will return the setted label
		/// </summary>
		/// <param name="label"></param>
		/// <returns></returns>
		public virtual Guid SetLabelToNextInstruction(string label)
		{
			if (nexlabelcomment == null)
			{
				nexlabelcomment = label;
				NextInstructionLabel = g.GNext();
			}
			else nexlabelcomment += " " + label;
			
			return NextInstructionLabel;
		}

		public virtual Guid ReserveInstructionLabel(string label)
		{
			var g = this.g.GNext();
			CurrentScope.ExpectedLabels[g] = label;
			return g;
		}
		public virtual void ApplyReservedLabel(Guid reservedlabel)
		{
			if (!string.IsNullOrEmpty(nexlabelcomment)) throw new InvalidOperationException("Labels conflict");
			string comment;
			if (!CurrentScope.ExpectedLabels.TryGetValue(reservedlabel, out comment)) throw new ArgumentException("Unknown label");
			CurrentScope.ExpectedLabels.Remove(reservedlabel);
			NextInstructionLabel = reservedlabel;
			nexlabelcomment = comment;
		}

		/// <summary>
		/// [IMPLEMENTATION_TIP] jumping to unset label will not cause an error if the label is reserved
		/// [IMPLEMENTATION_TIP] no jumps to outer or inner scopes are allowed, you can get as far as the END label
		/// </summary>
		/// <param name="label"></param>
		public abstract void Goto(Guid label);

		public abstract void UnstructuredGoto(Guid abslabel);

		/// <summary>
		/// [IMPLEMENTATION_TIP] jumping to unset label will not cause an error if the label is reserved
		/// [IMPLEMENTATION_TIP] no jumps to outers scopes are allowed, you can get as far as END label
		/// </summary>
		/// <param name="label"></param>
		public abstract void GotoIfZero(Guid label, H int_op);

		/// <summary>
		/// [IMPLEMENTATION_TIP] jumping to unset label will not cause an error if the label is reserved
		/// [IMPLEMENTATION_TIP] no jumps to outers scopes are allowed, you can get as far as END label
		/// </summary>
		/// <param name="label"></param>
		public abstract void GotoIfNotZero(Guid label, H int_op);

		/// <summary>
		/// [IMPLEMENTATION_TIP] jumping to unset label will not cause an error if the label is reserved
		/// [IMPLEMENTATION_TIP] no jumps to outers scopes are allowed, you can get as far as END label
		/// [IMPLEMENTATION_TIP] int_op >= 0
		/// </summary>
		/// <param name="label"></param>
		public abstract void GotoIfNotNegative(Guid label, H int_op);

		/// <summary>
		/// [IMPLEMENTATION_TIP] jumping to unset label will not cause an error if the label is reserved
		/// [IMPLEMENTATION_TIP] no jumps to outers scopes are allowed, you can get as far as END label
		/// [IMPLEMENTATION_TIP] int_op less than 0
		/// </summary>
		/// <param name="label"></param>
		public abstract void GotoIfNegative(Guid label, H int_op);
		#endregion

		#region [Scope Handling]

		/// <summary>
		/// </summary>
		/// <param name="definetype">true if the new scope will contains type definitions</param>
		/// <param name="namehint">
		/// [IMPLEMENTATION_TIP] adds a comment befor enter the scope on the compiled code
		/// </param>
		[ScopeChanger(Reason = "Creates and enters in a nested scope", ScopeName = "InnerScope_<scopelabel>")]
		public abstract void EnterNestedScope(bool definetype = false, string namehint = null);

		/// <summary>
		/// [IMPLEMENTATION_TIP] This should free any memory reserved in the scope that is about to leave.
		/// [IMPLEMENTATION_TIP] Unused labels at the end of the scope will generate warnings
		/// [IMPLEMENTATION_TIP] Unset label at the end of the scope generate critical errors
		/// </summary>
		[ScopeChanger(Reason = "Closes the current scope and returns to it's parent")]
		public abstract void LeaveScope();

		#endregion
	}
}
