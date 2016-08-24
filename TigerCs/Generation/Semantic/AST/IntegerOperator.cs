using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.Semantic.AST
{
	public abstract class IntegerOperator : BinaryOperator
	{
		public override bool CheckSemantics(ISemanticChecker sc, ErrorReport report)
		{
			throw new NotImplementedException();
		}
	}

	public class Add : IntegerOperator
	{
		protected override void Generate<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			throw new NotImplementedException();
		}
	}

	public class Sub : IntegerOperator
	{
		protected override void Generate<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			throw new NotImplementedException();
		}
	}

	public class Or : IntegerOperator
	{
		protected override void Generate<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			throw new NotImplementedException();
		}
	}

	public class And : IntegerOperator
	{
		protected override void Generate<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			throw new NotImplementedException();
		}
	}

	public class Mult : IntegerOperator
	{
		protected override void Generate<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			throw new NotImplementedException();
		}
	}

	public class Div : IntegerOperator
	{
		protected override void Generate<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			throw new NotImplementedException();
		}
	}

	public class Neg : IntegerOperator
	{
		protected override void Generate<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			throw new NotImplementedException();
		}
	}
}