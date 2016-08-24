using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.Semantic.AST
{
	public abstract class ComparisonOperator : BinaryOperator
	{
		public override bool CheckSemantics(ISemanticChecker sc, ErrorReport report)
		{
			throw new NotImplementedException();
		}
	}

	public class GreaterThan : ComparisonOperator
	{
		protected override void Generate<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			throw new NotImplementedException();
		}
	}

	public class GreaterEqualThan : ComparisonOperator
	{
		protected override void Generate<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			throw new NotImplementedException();
		}
	}

	public class LessThan : ComparisonOperator
	{
		protected override void Generate<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			throw new NotImplementedException();
		}
	}

	public class LessEqualThan : ComparisonOperator
	{
		protected override void Generate<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			throw new NotImplementedException();
		}
	}
}