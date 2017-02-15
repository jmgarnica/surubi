using System;
using System.Collections.Generic;
using System.Linq;
using TigerCs.CompilationServices;
using TigerCs.Generation.AST.Expressions;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Declarations
{
	public class FunctionDeclaration : IDeclaration
	{
		[Release]
		[NotNull]
		public IExpression Body { get; set; }

		[Release(true)]
		[NotNull]
		public List<ParameterDeclaration> Parameters { get; set; }

		public string Return { get; set; }

		[NotNull]
		public string FunctionName { get; set; }

		public FunctionInfo Func { get; private set; }

		TypeInfo _int, _void;

		#region [AST]

		public int column {	get; set; }

		public int line	{ get; set;	}

		public string Lex {	get; set; }

		public bool Pure { get; protected set; }

		#endregion

		public bool BindName(ISemanticChecker sc, ErrorReport report)
		{
			_void = sc.Void(report);

			Func = new FunctionInfo
			{
				Parameters = new List<Tuple<string, TypeInfo>>(),
				Name = FunctionName,
				Return = _void
			};

			foreach (var p in Parameters)
			{
				if(!p.CheckSemantics(sc, report))
					return false;

				Func.Parameters.Add(new Tuple<string, TypeInfo>(p.HolderName, p.Type));
			}

			if (!string.IsNullOrEmpty(Return))
			{

				MemberInfo mem;
				if (!sc.Reachable(TypeInfo.MakeTypeName(Return), out mem))
				{
					report.Add(new StaticError(line, column, $"Type {Return} is unaccessible", ErrorLevel.Error));
					return false;
				}

				Func.Return = mem as TypeInfo;
				if (Func.Return == null)
				{
					report.Add(new StaticError(line, column, $"The non-type member {mem.Name} was declared in a type namespace",
					                           ErrorLevel.Internal));
					return false;
				}

				if (Func.Return.Equals(_void) || Func.Return.Equals(sc.Null(report)))
				{

					report.Add(new StaticError(line, column, "Functions must return a value", ErrorLevel.Internal));
					return false;
				}
			}

			if (sc.DeclareMember(FunctionName, new MemberDefinition
			{
				line = line,
				column = column,
				Member = Func
			}))
				return true;


			report.Add(new StaticError(line, column, "A member with the same name already exist", ErrorLevel.Error));
			return false;
		}

		public bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{
			if (Func == null)
			{
				report.Add(new StaticError(line, column, "Binding phase required", ErrorLevel.Internal));
				return false;
			}

			_int = sc.Int(report);

			sc.EnterNestedScope(descriptors: new FunctionScopeDescriptor());
			foreach (var p in Parameters)
				if (!p.BindName(sc, report)) return false;

			if (!Body.CheckSemantics(sc, report, Func.Return))
				return false;

			if (!Body.Return.Equals(Func.Return) && (!Body.Return.Equals(sc.Null(report)) || Func.Return.Equals(_int)))
			{
				report.Add(new StaticError(Body.line, Body.column,
				                           $"The return type of the function {FunctionName} is {Func.Return}," +
				                           $" but it's body returns a value of type {Body.Return}",
				                           ErrorLevel.Error));
				return false;
			}

			sc.LeaveScope();

			return true;
		}

		public void DeclareFunction<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
			where T : class, IType<T, F>
			where F : class, IFunction<T, F>
			where H : class, IHolder
		{
			Func.BCMMember = cg.DeclareFunction(FunctionName,
			                                    Func.Return.Equals(_void)? (T)_int.BCMMember : (T)Func.Return.BCMMember,
			                                    (from p in Parameters
			                                     select new Tuple<string, T>(p.HolderName, (T)p.Type.BCMMember)).ToArray());
		}

		public void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
			where T : class, IType<T, F>
			where F : class, IFunction<T, F>
			where H : class, IHolder
		{
			cg.BindFunction((F)Func.BCMMember);
			foreach (var p in Parameters)
				p.GenerateCode(cg, report);

			Body.GenerateCode(cg, report);
			if(!Func.Return.Equals(_void))cg.Ret((H)Body.ReturnValue.BCMMember);
			else cg.Ret();
			cg.LeaveScope();
		}
	}
}
