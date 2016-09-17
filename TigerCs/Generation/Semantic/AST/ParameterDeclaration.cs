using System;
using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;
using TigerCs.Generation.Semantic;
using TigerCs.Generation.Semantic.AST;

namespace TigerCs
{
	public class ParameterDeclaration : Declaration
	{
		public string HolderType { get; set; }
				
		public override void BindName(ISemanticChecker sc, ErrorReport report)
		{
			throw new NotImplementedException();
		}

		public override bool CheckSemantics(ISemanticChecker sc, ErrorReport report)
		{
			throw new NotImplementedException();
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			throw new NotImplementedException();
		}
	}
}