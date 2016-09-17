using System;
using TigerCs.Generation.Semantic;
using TigerCs.Generation.Semantic.AST;

namespace TigerCs.CompilationServices
{
	public class BackupDefinition
	{
		public MemberInfo Member { get; set; }

		public Action<ISemanticChecker, ErrorReport> GeneratorStarter { get; set; }

		public FunctionDeclaration Generator { get; set; }
	}
}
