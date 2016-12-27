using System;
using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Expresions
{
	public abstract class BinaryOperator : Expresion
	{
		[NotNull]
		[Release]
		public IExpresion Rigth { get; set; }

		[NotNull]
		[Release]
		public IExpresion Left { get; set; }
	}

	public class EqualityOperator : BinaryOperator
	{
		public override bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{
			throw new NotImplementedException();
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			throw new NotImplementedException();
		}
	}
}