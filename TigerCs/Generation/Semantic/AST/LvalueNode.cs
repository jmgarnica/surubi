using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TigerCs.Generation.ByteCode;
using TigerCs.Generation.Semantic;

namespace TigerCs.Generation.Semantic.AST
{
	public abstract class Lvalue : Expresion
	{
		
	}

	public class Var : Lvalue
	{
		public override bool CheckSemantics(ISemanticChecker sp, ErrorReport report)
		{
			throw new NotImplementedException();
		}

		protected override void Generate<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			throw new NotImplementedException();
		}
	}

	public class ArrayAccess : Lvalue
	{
		Expresion expresion;

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

		protected override void Generate<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			throw new NotImplementedException();
		}
	}
}