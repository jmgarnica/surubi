using System;
using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Expresions
{
	public class IfThenElse : Expresion
	{
		public override bool CheckSemantics(ISemanticChecker sc, ErrorReport report)
		{
			throw new NotImplementedException();
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			//TODO: Generate Code
		}
	}
}