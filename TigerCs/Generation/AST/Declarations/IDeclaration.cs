using System.Collections.Generic;
using TigerCs.CompilationServices;

namespace TigerCs.Generation.AST.Declarations
{
	public interface IDeclaration : IASTNode
	{
		/// <summary>
		/// Binds the member info to the current scope, the member info is allow to be incomplete
		/// until the CheckSemantic method ends, if this method is called after CheckSemantic,
		/// as in a ParameterDeclaration inside a FunctionDeclaration, then the member info is
		/// not allow to be incomplete
		/// </summary>
		bool BindName(ISemanticChecker sc, ErrorReport report, List<string> same_scope_definitions = null);
	}
}