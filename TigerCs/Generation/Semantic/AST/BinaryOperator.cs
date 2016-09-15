using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.Semantic.AST
{
	public abstract class BinaryOperator : Expresion
	{
		public IExpresion Rigth { get; set; }
		public IExpresion Left { get; set; }

		public override void Dispose()
		{
			Rigth.Dispose();
			Left.Dispose();
			base.Dispose();
		}
	}

	public class EqualityOperator : BinaryOperator
	{
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