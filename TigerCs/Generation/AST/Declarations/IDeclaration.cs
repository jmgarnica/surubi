using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Declarations
{
	public interface IDeclaration : IASTNode
	{
		void BindName(ISemanticChecker sc, ErrorReport report);
	}
}