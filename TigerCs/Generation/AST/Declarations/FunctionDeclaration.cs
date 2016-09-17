using System;
using System.Collections.Generic;
using TigerCs.CompilationServices;
using TigerCs.Generation.AST.Expresions;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Declarations
{
	public class FunctionDeclaration : Declaration
	{
		public IExpresion Body { get; set; }
		public List<ParameterDeclaration> Parameters { get; set; }
		public FunctionInfo Func { get; private set; }

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
