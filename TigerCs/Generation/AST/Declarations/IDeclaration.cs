using TigerCs.CompilationServices;

namespace TigerCs.Generation.AST.Declarations
{
	public interface IDeclaration : IASTNode
	{
		void BindName(ISemanticChecker sc, ErrorReport report);
	}
}