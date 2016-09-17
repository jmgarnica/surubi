using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Declarations
{
	public abstract class Declaration : IASTNode
	{
		public int column { get; set; }
		public bool CorrectSemantics { get; protected set; }
		public string Lex { get; set; }
		public int line { get; set; }
		public string Name { get; set; }
		public abstract void BindName(ISemanticChecker sc, ErrorReport report);
		public abstract bool CheckSemantics(ISemanticChecker sc, ErrorReport report);

		public abstract void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		   where T : class, IType<T, F>
		   where F : class, IFunction<T, F>
		   where H : class, IHolder;
	}
}