//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using TigerCs.Generation;
//using TigerCs.Generation.ByteCode;
//using TigerCs.Generation.Scopes;

//namespace TigerCs.Emitters.CSharp
//{
//	public class CSharpEmitter : ITigerEmitter
//	{
//		public TypeInfo VoidType { get; private set; }
//		public TypeInfo Int { get; private set; }
//		public TypeInfo String { get; private set; }
//		public HolderInfo Nill { get; private set; }
//		public IDictionary<string, FunctionInfo> STD { get; private set; }

//		#region [VARS]
//		Stack<string> commonlavel;
//		TextWriter writer;
//		ErrorReport report;
//		int indentationlevel;
//		Dictionary<int, IHolder> integerconstants;
//		Dictionary<string, IHolder> stringconstants;
//		long tempvarcount;
//		const string callstack = "callstack";
//		#endregion

//		#region [Control]

//		public CSharpEmitter()
//		{
//			VoidType = new TypeInfo();
//			VoidType.Type = new CSharpType("void");
//			Int = new TypeInfo();
//			Int.Type = new CSharpType("int");
//			String = new TypeInfo();
//			String.Type = new CSharpType("string");
//			Nill = new HolderInfo
//			{
//				Bounded = true,
//				Holder = new CSharpHolder
//				{
//					access = "null",
//					Assignable = false,
//					Nested = null,
//					Type = new CSharpType("object")
//				},
//				Type = new TypeInfo
//				{
//					Bounded = true,
//					Members = new Dictionary<string, TypeInfo>(),
//					Name = "object",
//					Type = new CSharpType("object")
//				},
//				Name = "nill"
//			};
//		}

//		public void IncludeStandarFunction(string standar)
//		{
//			throw new NotImplementedException();
//			//TODO: Standarts hav to be initialated befor "initialize" but not bound
//		}

//		public void InitializeCodeGeneration(TextWriter writer, ErrorReport report, bool emit = true)
//		{
//			this.writer = writer;
//			this.report = report;
//			indentationlevel = 0;
//			STD = new Dictionary<string, FunctionInfo>();
//			commonlavel = new Stack<string>();
//			integerconstants = new Dictionary<int, IHolder>();
//			stringconstants = new Dictionary<string, IHolder>();
//			tempvarcount = 0;
//			RootScope = new TigerScope();
//			CurrentScope = RootScope;

//			WriteHeaders();

//			foreach (var item in STD)
//			{
//				RootScope.DeclareMember(item.Key, item.Value);
//			}
//		}
//		public void End()
//		{
//			LeaveScope();
//			WriteFooter();
//			writer = null;
//			report = null;
//		}

//		public void Comment(string comment)
//		{
//			Indent();
//			writer.WriteLine("/*" + comment + "*/");
//		}

//		public void BlankLine()
//		{
//			writer.WriteLine();
//		}

//		public void EmitError(int code)
//		{
//			Indent();
//			writer.WriteLine("throw new InvalidOperationException(\"error code:" + code + "\");");
//		}
//		public void EmitError(string message)
//		{
//			Indent();
//			writer.WriteLine("throw new InvalidOperationException(" + message + ");");
//		}
//		#endregion

//		#region [Bound]

//		public IHolder AddConstant(int value)
//		{
//			IHolder v;
//			if (integerconstants.TryGetValue(value, out v)) return v;
//			v = integerconstants[value] = new CSharpHolder
//			{
//				access = value.ToString(),
//				Assignable = false,
//				Nested = null,
//				Type = Int.Type
//			};

//			return v;
//		}
//		public IHolder AddConstant(string value)
//		{
//			IHolder v;
//			if (stringconstants.TryGetValue(value, out v)) return v;
//			v = stringconstants[value] = new CSharpHolder
//			{
//				access = value.ToString(),
//				Assignable = false,
//				Nested = null,
//				Type = String.Type
//			};

//			return v;
//		}
//		public IHolder BoundVar(IType tigertype, string name = null)
//		{
//			//TODO: mejorar exepciones
//			if (!(tigertype is CSharpType)) throw new ArgumentException("Invalid Convination");
//			CSharpType t = tigertype as CSharpType;
//			CSharpHolder var = new CSharpHolder { Assignable = true, Nested = null, Type = tigertype };
//			if (tigertype.Equal(VoidType.Type)) throw new NotImplementedException("to error report: critical");
//			Indent();
//			if (name == null)
//			{
//				tempvarcount++;
//				name = "tmpvar_" + tempvarcount.ToString();
//			}

//			var.access = name;
//			MemberInfo m;
//			if (CurrentScope.Reachable(name, out m)) throw new ArgumentException("var name duplicated");
//			if (tigertype.Equal(String.Type))
//				writer.WriteLine(t.DotNetName + " " + name + " = \"\";");
//			else writer.WriteLine(t.DotNetName + " " + name + " = new " + t.DotNetName + "();");
//			return var;
//		}


//		public IFunction AheadFunctionDeclaration(string name, IType returntype, params IType[] args)
//		{
//			throw new NotImplementedException();
//		}

//		public IFunction BoundFunction(string name, IType returntype, params IType[] args)
//		{
//			throw new NotImplementedException();
//		}

//		public void BoundFunction(IFunction aheadedfunction)
//		{
//			throw new NotImplementedException();
//		}


//		public IType AheadTypeDeclaration(string name)
//		{
//			throw new NotImplementedException();
//		}

//		#endregion

//		#region [General Instructions]
//		public void InstrAssing(IHolder dest_nonconst, IHolder value)
//		{
//			throw new NotImplementedException();
//		}

//		public void InstrAdd(IHolder dest_nonconst, IHolder op1, IHolder op2)
//		{
//			Indent();
//			writer.WriteLine(((CSharpHolder)dest_nonconst).access + " = " + ((CSharpHolder)op1).access + " + " + ((CSharpHolder)op2).access + ";");
//		}
//		public IHolder InstrAdd_TempBound(IHolder op1, IHolder op2)
//		{
//			throw new NotImplementedException();
//		}

//		public void InstrSub(IHolder dest_nonconst, IHolder op1, IHolder op2)
//		{
//			Indent();
//			writer.WriteLine(((CSharpHolder)dest_nonconst).access + " = " + ((CSharpHolder)op1).access + " - " + ((CSharpHolder)op2).access + ";");
//		}
//		public IHolder InstrSub_TempBound(IHolder op1, IHolder op2)
//		{
//			var tmp = BoundVar(Int.Type);
//			Indent();
//			writer.WriteLine(((CSharpHolder)tmp).access + " = " + ((CSharpHolder)op1).access + " - " + ((CSharpHolder)op2).access + ";");
//			return tmp;
//		}

//		public void InstrMult(IHolder dest_nonconst, IHolder op1, IHolder op2)
//		{
//			Indent();
//			writer.WriteLine(((CSharpHolder)dest_nonconst).access + " = " + ((CSharpHolder)op1).access + " * " + ((CSharpHolder)op2).access + ";");
//		}
//		public IHolder InstrMult_TempBound(IHolder op1, IHolder op2)
//		{
//			throw new NotImplementedException();
//		}

//		public void InstrDiv(IHolder dest_nonconst, IHolder op1, IHolder op2)
//		{
//			Indent();
//			writer.WriteLine(((CSharpHolder)dest_nonconst).access + " = " + ((CSharpHolder)op1).access + " / " + ((CSharpHolder)op2).access + ";");
//		}
//		public IHolder InstrDiv_TempBound(IHolder op1, IHolder op2)
//		{
//			throw new NotImplementedException();
//		}

//		public void InstrInverse(IHolder dest_nonconst, IHolder op1)
//		{
//			Indent();
//			writer.WriteLine(((CSharpHolder)dest_nonconst).access + " = -" + ((CSharpHolder)op1).access + ";");
//		}
//		public IHolder InstrInverse_TempBound(IHolder op1)
//		{
//			throw new NotImplementedException();
//		}

//		public void InstrRefEq(IHolder dest_nonconst, IHolder op1, IHolder op2)
//		{
//			Indent();
//			writer.WriteLine(((CSharpHolder)dest_nonconst).access + " = GC.ReferenceEquals(" + ((CSharpHolder)op1).access + ", " + ((CSharpHolder)op2).access + ") ? 1 : 0;");
//		}
//		public IHolder InstrRefEq_TempBound(IHolder op1, IHolder op2)
//		{
//			throw new NotImplementedException();
//		}

//		#endregion

//		#region [Call]
//		/// <summary>
//		/// Push a parameter in a stack for latter use whith call. 
//		/// [IMPLEMENTATION_TIP] Ending a program with a non-empty stack will result in error.
//		/// </summary>
//		/// <param name="holder"></param>
//		public void Param(IHolder holder)
//		{
//			Indent();
//			writer.WriteLine(callstack+".Push(" + ((CSharpHolder)holder).access + ");");
//		}

//		/// <summary>
//		/// Enters in a function with the <paramref name="paramcount"/> parameters on the top of the stack as it's parameters.
//		/// [IMPLEMENTATION_TIP] The parameters are deleted from the stack.
//		/// </summary>
//		/// <param name="function"></param>
//		/// <param name="paramcount"></param>
//		public void Call(FunctionInfo function, int paramcount, IHolder returnval = null)
//		{
//			Indent();
//			if (returnval != null) writer.Write(((CSharpHolder)returnval).access + " = ");
//			writer.Write(((CSharpFunction)function.Function).access + "(");
//			for (int i = 0; i < paramcount; i++)
//			{
//				writer.Write("((" + ((CSharpType)function.Parameters[i].Item2.Type).DotNetName + ")"+callstack+".Pop())" + (i == paramcount - 1 ? "" : ", "));
//			}
//			writer.WriteLine(");");
//		}

//		/// <summary>
//		/// All function path must end with one form of return. An error will be generated if not.
//		/// </summary>
//		/// <param name="value"></param>
//		public void Ret(IHolder value)
//		{
//			throw new NotImplementedException();
//		}

//		/// <summary>
//		/// All function path must end with one form of return. An error will be generated if not.
//		/// </summary>
//		public void Ret()
//		{
//			throw new NotImplementedException();
//		}
//		#endregion

//		#region [Scope Handling]

//		/// <summary>
//		/// This is the common prefix of all lavels in the currentscope.
//		/// </summary>
//		public string ScopeCommonLabel
//		{
//			get
//			{
//				if (commonlavel.Count == 0) return "";
//				return commonlavel.Aggregate((i, j) => i + "_" + j) + "_";
//			}
//		}

//		public TigerScope RootScope { get; private set; }
//		public TigerScope CurrentScope { get; private set; }

//		/// <summary>
//		/// [IMPLEMENTATION_TIP] Add <paramref name="newscopelabel"/> to ScopeCommonLavel to make the common label of the new scope
//		/// </summary>
//		/// <param name="newscopelabel"></param>
//		/// <param name="definetype">true if the new scope will define a new type</param>
//		[ScopeChanger(Reason = "Creates and enters in a nested scope", ScopeName = "InnerScope")]
//		public void EnterNestedScope(string newscopelabel, bool definetype, string scopelabel = null, bool emit = true)
//		{
//			throw new NotImplementedException();
//		}

//		/// <summary>
//		/// [IMPLEMENTATION_TIP] This should free any memory reserved in the scope that is about to leave.
//		/// [IMPLEMENTATION_TIP] Remove the last part of ScopeCommonLavel, separators are TigerScope.ScopeNameSeparator
//		/// </summary>
//		public void LeaveScope(bool emit = true)
//		{
			
//			Indent();
//			writer.WriteLine(ScopeCommonLabel+ "END: ;");
//			indentationlevel--;
//			Indent();
//			writer.WriteLine("}");
//			writer.WriteLine();
//		}
//		#endregion

//		#region [GOTO]

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
//			writer.WriteLine(ScopeCommonLabel + label + ":");
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
//			writer.WriteLine("goto "+ ScopeCommonLabel + label + ";");
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
//			writer.WriteLine("if ("+((CSharpHolder)op1).access+" == 0)goto " + ScopeCommonLabel + label + ";");
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
//			writer.WriteLine("if (" + ((CSharpHolder)op1).access + " != 0)goto " + ScopeCommonLabel + label + ";");
//		}

//		#endregion

//		#region [STD/Utils Writers]

//		public void Indent()
//		{
//			for (int i = 0; i < indentationlevel; i++)
//				writer.Write("	");
//		}

//		void WriteUsings()
//		{
//			//TODO: Usings
//			writer.WriteLine("#region [USINGS]");
//			writer.WriteLine();
//			writer.WriteLine("using System;");
//			writer.WriteLine("using System.Collections.Generic;");
//			writer.WriteLine("using System.IO;");
//			writer.WriteLine("using System.Linq;");
//			writer.WriteLine("using System.Text;");
//			writer.WriteLine();
//			writer.WriteLine("#endregion");
//		}


//		void Register_WriteSTD()
//		{
//			Indent();
//			writer.WriteLine("#region [STD]");
//			writer.WriteLine();

//			//print_write
//			Indent();
//			writer.WriteLine("//print = Console.WriteLine");
//			writer.WriteLine();
//			//print_register
//			FunctionInfo info = new FunctionInfo { Bounded = true, Name = "print" };
//			info.Parameters = new List<Tuple<string, TypeInfo>> { new Tuple<string, TypeInfo>("s", String) };
//			info.Function = new CSharpFunction("Console.WriteLine", VoidType.Type);
//			STD["print"] = info;

//			//printi_write
//			Indent();
//			writer.WriteLine("//printi = Console.WriteLine");
//			writer.WriteLine();
//			//printi_register
//			info = new FunctionInfo { Bounded = true, Name = "printi" };
//			info.Parameters = new List<Tuple<string, TypeInfo>> { new Tuple<string, TypeInfo>("i", Int) };
//			info.Function = new CSharpFunction("Console.WriteLine", VoidType.Type);
//			STD["printi"] = info;

//			//flush_write
//			Indent();
//			writer.WriteLine("public static void flush() { }");
//			writer.WriteLine();
//			//flush_register
//			info = new FunctionInfo { Bounded = true, Name = "flush" };
//			info.Parameters = new List<Tuple<string, TypeInfo>>();
//			info.Function = new CSharpFunction("flush", VoidType.Type);
//			STD["flush"] = info;

//			//getchar_write
//			Indent();
//			writer.WriteLine("public static string getchar() => ((char)Console.Read()).ToString();");
//			writer.WriteLine();
//			//getchar_register
//			info = new FunctionInfo { Bounded = true, Name = "getchar" };
//			info.Parameters = new List<Tuple<string, TypeInfo>>();
//			info.Function = new CSharpFunction("getchar", String.Type);
//			STD["getchar"] = info;

//			//ord_write
//			Indent();
//			writer.WriteLine("public static int ord(string s) => (string.IsNullOrEmpty(s)? -1: s[0]);");
//			writer.WriteLine();
//			//ord_register
//			info = new FunctionInfo { Bounded = true, Name = "ord" };
//			info.Parameters = new List<Tuple<string, TypeInfo>> { new Tuple<string, TypeInfo>("s", String)};
//			info.Function = new CSharpFunction("ord", Int.Type);
//			STD["ord"] = info;

//			//chr_write
//			Indent();
//			writer.WriteLine("public static string chr(int i)");
//			Indent();
//			writer.WriteLine("{");
//			indentationlevel++;
//			Indent();
//			writer.WriteLine("if ( i < 0 || i > 127) throw new InvalidOperationException(\"integer value out of range[0,127]\");");
//			Indent();
//			writer.WriteLine("return new string((char)i, 1);");
//			indentationlevel--;
//			Indent();
//			writer.WriteLine("}");
//			writer.WriteLine();
//			//chr_register
//			info = new FunctionInfo { Bounded = true, Name = "chr" };
//			info.Parameters = new List<Tuple<string, TypeInfo>> { new Tuple<string, TypeInfo>("i", Int) };
//			info.Function = new CSharpFunction("chr", String.Type);
//			STD["chr"] = info;

//			Indent();
//			writer.WriteLine("#endregion");
//			writer.WriteLine();
//		}

//		void WriteHeaders()
//		{
//			WriteUsings();
//			writer.WriteLine();
//			writer.WriteLine("namespace CSHARPTESTTIGER");
//			writer.WriteLine("{");
//			writer.WriteLine();

//			indentationlevel++;
//			Indent();

//			writer.WriteLine("public class TIGERMAIN");
//			Indent();
//			writer.WriteLine("{");
//			indentationlevel++;

//			Indent();
//			writer.WriteLine("static Stack<object> "+callstack+" = new Stack<object>();");
//			writer.WriteLine();

//			Register_WriteSTD();

//			Indent();
//			writer.WriteLine("static void Main(string[] args)");
//			Indent();
//			writer.WriteLine("{");
//			indentationlevel++;
//		}

//		void WriteFooter()
//		{
//			WritePendings();

//			indentationlevel = 1;
//			Indent();
//			writer.WriteLine("}");

//			indentationlevel = 0;
//			Indent();
//			writer.WriteLine("}");
//		}

//		void WritePendings()
//		{
//			//TODO: Type Declarations
//		}

		

//		#endregion
//	}
//}
