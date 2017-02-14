using TigerCs.CompilationServices;

namespace TigerCs.Generation.AST.Declarations
{
	public interface IDeclaration : IASTNode
	{
		bool BindName(ISemanticChecker sc, ErrorReport report);
	}
}