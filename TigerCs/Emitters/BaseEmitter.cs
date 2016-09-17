//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using TigerCs.Generation;
//using TigerCs.Generation.ByteCode;
//using TigerCs.Generation.Scopes;

//namespace TigerCs.Emitters
//{
//	public abstract class BaseTexEmitter : ITigerEmitter
//	{
//		#region [STD]
//		public TypeInfo VoidType { get; protected set; }
//		public TypeInfo Int { get; protected set; }
//		public TypeInfo String { get; protected set; }
//		public HolderInfo Nill { get; protected set; }
//		public IDictionary<string, FunctionInfo> STD { get; protected set; }
//		#endregion

//		#region [VARS]
//		//protected
//		protected Stack<string> CommonLabel;
//		protected MultiTrackWriter Writer;
//		protected ErrorReport Report;
//		protected Dictionary<int, IHolder> IntegerConstants;
//		protected Dictionary<string, IHolder> StringConstants;
//		protected long TemporalVariableCount;
//		protected const string CallStackName = "__callstack__";
//		protected bool labelwaitingistruction;
//		protected string nextlabel;
//		//private
//		private Dictionary<string, bool> standarsignals;
//		#endregion

//		public TigerScope RootScope { get; protected set; }
//		public TigerScope CurrentScope { get; protected set; }

//		public BaseTexEmitter()
//		{
//		}

//		public void InitializeSemanticCheck()
//		{
//			STD = new Dictionary<string, FunctionInfo>();
//			RootScope = new TigerScope();
//			CurrentScope = RootScope;

//			RegisterSTD();
//			standarsignals = new Dictionary<string, bool>();
//			foreach (var item in STD)
//				standarsignals[item.Key] = false;
//		}

//		public void IncludeStandarFunction(string standar)
//		{
//			standarsignals[standar] = true;
//		}

//		public void InitializeCodeGeneration(IWriter writer, ErrorReport report)
//		{
//			CommonLabel = new Stack<string>();
//			Writer = new MultiTrackWriter(writer);
//			Report = report;
//			IntegerConstants = new Dictionary<int, IHolder>();
//			StringConstants = new Dictionary<string, IHolder>();
//			TemporalVariableCount = 0;
//			labelwaitingistruction = false;
//			nextlabel = "";

//			RootScope = new TigerScope();
//			CurrentScope = RootScope;

//			EmitHeaders();
//			EmitSTD();
//			EmitFooter();
//		}

//		public virtual void End()
//		{
//			LeaveScope();
//			Writer.OrderedFlush();
//			Writer = null;
//			Report = null;
//		}

//		#region [Control]

//		public abstract void Comment(string comment);

//		public abstract void BlankLine();

//		public abstract void EmitError(int code);
//		public abstract void EmitError(string message);

//		#endregion

//		#region [Bound]
//		public abstract IHolder AddConstant(int value);
//		public abstract IHolder AddConstant(string value);
//		public abstract IHolder BoundVar(IType tigertype, string name = null);

//		public abstract IFunction BoundFunction(string name, IType returntype, params IType[] args);
//		public abstract void BoundFunction(IFunction aheadedfunction);

//		public abstract IFunction AheadFunctionDeclaration(string name, IType returntype, params IType[] args);
//		public abstract IType AheadTypeDeclaration(string name);
//		#endregion

//		#region [General Instructions]
//		public abstract void InstrAssing(IHolder dest_nonconst, IHolder value);

//		public abstract void InstrAdd(IHolder dest_nonconst, IHolder op1, IHolder op2);
//		public abstract IHolder InstrAdd_TempBound(IHolder op1, IHolder op2);

//		public abstract void InstrSub(IHolder dest_nonconst, IHolder op1, IHolder op2);
//		public abstract IHolder InstrSub_TempBound(IHolder op1, IHolder op2);

//		public abstract void InstrMult(IHolder dest_nonconst, IHolder op1, IHolder op2);
//		public abstract IHolder InstrMult_TempBound(IHolder op1, IHolder op2);

//		public abstract void InstrDiv(IHolder dest_nonconst, IHolder op1, IHolder op2);
//		public abstract IHolder InstrDiv_TempBound(IHolder op1, IHolder op2);

//		public abstract void InstrInverse(IHolder dest_nonconst, IHolder op1);
//		public abstract IHolder InstrInverse_TempBound(IHolder op1);

//		public abstract void InstrRefEq(IHolder dest_nonconst, IHolder op1, IHolder op2);
//		public abstract IHolder InstrRefEq_TempBound(IHolder op1, IHolder op2);

//		#endregion

//		#region [Call]
//		/// <summary>
//		/// Push a parameter in a stack for latter use whith call. 
//		/// [IMPLEMENTATION_TIP] Ending a program with a non-empty stack will result in error.
//		/// </summary>
//		/// <param name="holder"></param>
//		public abstract void Param(IHolder holder);

//		/// <summary>
//		/// Enters in a function with the <paramref name="paramcount"/> parameters on the top of the stack as it's parameters.
//		/// [IMPLEMENTATION_TIP] The parameters are deleted from the stack.
//		/// </summary>
//		/// <param name="function"></param>
//		/// <param name="paramcount"></param>
//		public abstract void Call(FunctionInfo function, int paramcount, IHolder returnval = null);

//		/// <summary>
//		/// All function path must end with one form of return. An error will be generated if not.
//		/// </summary>
//		/// <param name="value"></param>
//		public abstract void Ret(IHolder value);

//		/// <summary>
//		/// All function path must end with one form of return. An error will be generated if not.
//		/// </summary>
//		public abstract void Ret();
//		#endregion

//		#region [Scope Handling]

//		/// <summary>
//		/// This is the common prefix of all lavels in the currentscope.
//		/// </summary>
//		public string ScopeCommonLabel
//		{
//			get
//			{
//				if (CommonLabel.Count == 0) return "";
//				return CommonLabel.Aggregate((i, j) => i + TigerScope.ScopeNameSeparator + j) + TigerScope.ScopeNameSeparator;
//			}
//		}

//		/// <summary>
//		/// [IMPLEMENTATION_TIP] Add <paramref name="newscopelabel"/> to ScopeCommonLavel to make the common label of the new scope
//		/// </summary>
//		/// <param name="newscopelabel"></param>
//		/// <param name="definetype">true if the new scope will define a new type</param>
//		[ScopeChanger(Reason = "Creates and enters in a nested scope", ScopeName = "InnerScope")]
//		public void EnterNestedScope(string newscopelabel, bool definetype, string scopelabel = null)
//		{
//			throw new NotImplementedException();
//		}

//		/// <summary>
//		/// [IMPLEMENTATION_TIP] This should free any memory reserved in the scope that is about to leave.
//		/// [IMPLEMENTATION_TIP] Remove the last part of ScopeCommonLavel, separators are TigerScope.ScopeNameSeparator
//		/// </summary>
//		public void LeaveScope()
//		{

//			Indent();
//			Writer.WriteLine(ScopeCommonLabel + "END: ;");
//			IndentationLevel--;
//			Indent();
//			Writer.WriteLine("}");
//			Writer.WriteLine();
//		}
//		#endregion

//		#region [GOTO & Labeling]

//		/// <summary>
//		/// [IMPLEMENTATION_TIP] On labels collision returns the actual label;
//		/// [IMPLEMENTATION_TIP] "END" label will mark the end of the scope automatically
//		/// [IMPLEMENTATION_TIP] Duplicated label generate critical errors
//		/// </summary>
//		/// <param name="label"></param>
//		/// <returns></returns>
//		public string SetLabelToNextInstruction(string label)
//		{
//			Indent();
//			Writer.WriteLine(ScopeCommonLabel + label + ":");
//			return label;
//			//TODO: cumplir con las verificaciones
//		}

//		/// <summary>
//		/// [IMPLEMENTATION_TIP] jumping to unset label will not cause an error, the label will be buffered until it's assignment
//		/// [IMPLEMENTATION_TIP] no jumps to outers scopes are allowed, you can get as far as END label
//		/// </summary>
//		/// <param name="label"></param>
//		public void Goto(string label)
//		{
//			Indent();
//			Writer.WriteLine("goto " + ScopeCommonLabel + label + ";");
//		}

//		/// <summary>
//		/// [IMPLEMENTATION_TIP] jumping to unset label will not cause an error, the label will be buffered until it's assignment
//		/// [IMPLEMENTATION_TIP] no jumps to outers scopes are allowed, you can get as far as END label
//		/// </summary>
//		/// <param name="label"></param>
//		/// <param name="op1"></param>
//		public void GotoIfZero(string label, IHolder op1)
//		{
//			if (!((CSharpHolder)op1).Type.Equal(Int.Type)) throw new ArgumentException("mover a reporte");
//			Indent();
//			Writer.WriteLine("if (" + ((CSharpHolder)op1).access + " == 0)goto " + ScopeCommonLabel + label + ";");
//		}

//		/// <summary>
//		/// [IMPLEMENTATION_TIP] jumping to unset label will not cause an error, the label will be buffered until it's assignment
//		/// [IMPLEMENTATION_TIP] no jumps to outers scopes are allowed, you can get as far as END label
//		/// </summary>
//		/// <param name="label"></param>
//		/// <param name="op1"></param>
//		public void GotoIfNotZero(string label, IHolder op1)
//		{
//			if (!((CSharpHolder)op1).Type.Equal(Int.Type)) throw new ArgumentException("mover a reporte");
//			Indent();
//			Writer.WriteLine("if (" + ((CSharpHolder)op1).access + " != 0)goto " + ScopeCommonLabel + label + ";");
//		}

//		#endregion

//		#region [Private & Abstract Delegations]

//		protected abstract void RegisterSTD();

//		protected abstract void EmitSTD();

//		protected abstract void EmitHeaders();

//		protected abstract void EmitFooter();

//		protected abstract void AddBlankIstruction();

//		#endregion
//	}

//	/// <summary>
//	/// Represents the state of a emitter for debuging information
//	/// </summary>
//	public enum EmitterState
//	{
//		Constructed,
//		InitializedSemanticCheck,
//		InitializedCodeGeneration,
//		Finalized
//	}
//}
