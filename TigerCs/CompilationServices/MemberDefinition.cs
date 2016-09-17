using System;
using TigerCs.Generation;
using TigerCs.Generation.AST.Declarations;

namespace TigerCs.CompilationServices
{
	public class MemberDefinition
	{
		public MemberInfo Member { get; set; }

		public Action<ISemanticChecker, ErrorReport> GeneratorStarter { get; set; }

		public FunctionDeclaration Generator { get; set; }

		
	}
}
