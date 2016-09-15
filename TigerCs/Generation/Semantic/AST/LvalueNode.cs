using System;
using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.Semantic.AST
{
	public class Var : Expresion
	{
		public override bool CheckSemantics(ISemanticChecker sp, ErrorReport report)
		{
			throw new NotImplementedException();
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			throw new NotImplementedException();
		}
	}

	public class ArrayAccess : Expresion
	{
		IExpresion expresion;

		public override bool CheckSemantics(ISemanticChecker sp, ErrorReport report)
		{
			if (expresion.CheckSemantics(sp, report))
			{
				//if (!expresion.Return.Type.Array)
				//{
				//	report.Add(new TigerStaticError(line, column, "array access to non-array type", ErrorLevel.Error, Lex));
				//	return false;
				//}
				//TODO: array underlaying type
			}
			return false;
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			throw new NotImplementedException();
		}
	}
}