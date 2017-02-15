using TigerCs.CompilationServices;
using TigerCs.Generation.AST.Expressions;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Declarations
{
	public class VarDeclaration : IDeclaration
	{
		[NotNull]
		[Release]
		public IExpression Init { get; set; }

		public string HolderType { get; set; }

		[NotNull]
		public string HolderName { get; set; }

		public HolderInfo Holder { get; protected set; }

		Assign inner;

		#region [AST]

		public int column { get; set; }

		public int line { get; set; }

		public string Lex { get; set; }

		public bool Pure { get; protected set; }

		#endregion

		public bool BindName(ISemanticChecker sc, ErrorReport report)
		{
			return true;
		}

		public virtual bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{
			TypeInfo t = null;
			if (!string.IsNullOrEmpty(HolderType))
			{
				MemberInfo mem;
				if (!sc.Reachable(TypeInfo.MakeTypeName(HolderType), out mem))
				{
					report.Add(new StaticError(line, column, $"Type {HolderType} is unaccessible", ErrorLevel.Error));
					return false;
				}

				t = mem as TypeInfo;
				if (t == null)
				{
					report.Add(new StaticError(line, column, $"The non-type member {mem.Name} was declared in a type namespace", ErrorLevel.Internal));
					return false;
				}
			}

			emptyVar e;
			inner = new Assign
			{
				line = line,
				column = column,
				Lex = Lex,
				Target = e = new emptyVar
				{
					line = line,
					column = column,
					Lex = Lex,
					Name = HolderName,
					Type = t
				},
				Source = Init
			};

			bool result = inner.CheckSemantics(sc, report, expected);
			Holder = e.Holder;
			return result;
		}

		public virtual void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
			where T : class, IType<T, F>
			where F : class, IFunction<T, F>
			where H : class, IHolder
		{
			Holder.BCMMember = cg.BindVar((T)Holder.Type.BCMMember, name: HolderName);
			inner.GenerateCode(cg, report);
		}

		class emptyVar : Var
		{
			public TypeInfo Type { private get; set; }

			public HolderInfo Holder { get; private set; }

			public override bool CheckSemantics(ISemanticChecker sp, ErrorReport report, TypeInfo expected = null)
			{
				if (Type == null && (expected == null || expected.Equals(sp.Void(report)) || expected.Equals(sp.Null(report))))
				{
					report.Add(new StaticError(line, column, $"The type of variable {Name} can't be inferred", ErrorLevel.Error));
					return false;
				}

				if (sp.DeclareMember(Name, new MemberDefinition
				                     {
					                     column = column,
					                     line = line,
					                     Member = Holder = new HolderInfo
					                              {
						                              Name = Name,
						                              Type = Type ?? expected
					                              }
				                     }))
					return base.CheckSemantics(sp, report, expected);

				report.Add(new StaticError(line, column, "A member with the same name already exist", ErrorLevel.Error));
				return false;
			}
		}
	}
}