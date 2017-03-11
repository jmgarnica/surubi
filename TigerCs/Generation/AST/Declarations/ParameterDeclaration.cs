using TigerCs.CompilationServices;
using TigerCs.CompilationServices.AutoCheck;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Declarations
{
	public class ParameterDeclaration : IDeclaration
	{
		public int Position { get; set; }

		public string HolderType { get; set; }

		[NotNull]
		public string HolderName { get; set; }

		public HolderInfo Holder { get; protected set; }

		public TypeInfo Type { get; protected set; }

		#region [AST]

		public int column { get; set; }

		public int line { get; set; }

		public string Lex { get; set; }

		public bool Pure { get; protected set; }

		#endregion

		public virtual bool BindName(ISemanticChecker sc, ErrorReport report)
		{
			if (Type == null)
			{
				report.Add(new StaticError(line, column, "Semantic checking phase required", ErrorLevel.Internal));
				return false;
			}

			Holder = new HolderInfo
			{
				Name = HolderName,
				Type = Type
			};

			if (sc.DeclareMember(HolderName, new MemberDefinition { line = line, column = column, Member = Holder })) return true;

			report.Add(new StaticError(line, column, "A member with the same name already exist", ErrorLevel.Error));
			return false;
		}

		public virtual bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{
			MemberInfo mem;
			if (!sc.Reachable(TypeInfo.MakeTypeName(HolderType), out mem))
			{
				report.Add(new StaticError(line, column, $"Type {HolderType} is unaccessible", ErrorLevel.Error));
				return false;
			}

			Type = mem as TypeInfo;

			if (Type != null) return true;

			report.Add(new StaticError(line, column, $"The non-type member {mem.Name} was declared in a type namespace", ErrorLevel.Internal));
			return false;
		}

		public void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
			where T : class, IType<T, F>
			where F : class, IFunction<T, F>
			where H : class, IHolder
		{
			Holder.BCMMember = cg.GetParam(Position);
		}
	}
}