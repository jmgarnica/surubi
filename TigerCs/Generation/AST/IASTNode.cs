using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST
{
	public interface IASTNode
	{
		int column { get; set; }
		int line { get; set; }
		string Lex { get; set; }
		bool CorrectSemantics { get; }
		bool CheckSemantics(ISemanticChecker sc, ErrorReport report);
		void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
			where T : class, IType<T, F>
			where F : class, IFunction<T, F>
			where H : class, IHolder;
	}
}
