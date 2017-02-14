using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Declarations
{
	public abstract class TypeDeclaration : IDeclaration
	{
		public TypeInfo Type { get; protected set; }
		public string TypeName { get; set; }

		/// <summary>
		/// Set after bind name
		/// </summary>
		[NotNull]
		public string[] Dependencies { get; protected set; }

		public int column { get; set; }

		public int line { get; set; }

		public string Lex { get; set; }

		public bool CorrectSemantics { get; private set; }

		public bool Pure { get; protected set; }

		public abstract bool BindName(ISemanticChecker sc, ErrorReport report);

		public abstract bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null);

		public abstract void DeclareType<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
			where T : class, IType<T, F>
			where F : class, IFunction<T, F>
			where H : class, IHolder;

		public abstract void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
			where T : class, IType<T, F>
			where F : class, IFunction<T, F>
			where H : class, IHolder;
	}
}