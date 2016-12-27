#define ENFORCE_RETURN_TYPE_CHECK
using System;
using System.Collections.Generic;
using TigerCs.CompilationServices;
using TigerCs.Generation.AST.Declarations;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Expresions
{
	public class Let : Expresion
	{
		[Release(true)]
		public DeclarationList<IDeclarationList<IDeclaration>> Declarations { get; set; }

		[Release]
		public IExpresion Body { get; set; }

		List<TypeInfo> declaredhere;

		public override bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{

			//TODO: put this everywhere
#if ENFORCE_RETURN_TYPE_CHECK
			if (Body != null && declaredhere.Contains(Body.Return))
			{
				report.Add(new StaticError(line, column, $"The return type {Body.Return} is not visible in the outer scope",
				                           ErrorLevel.Error));
				return false;
			}
#endif
				throw new NotImplementedException();
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			//TODO: Generate Code
		}
	}
}