using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TigerCs.CompilationServices;
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
		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			throw new NotImplementedException();
		}
	}

	public class Sub : IntegerOperator
	{
		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			throw new NotImplementedException();
		}
	}

	public class Or : IntegerOperator
	{
		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			throw new NotImplementedException();
		}
	}

	public class And : IntegerOperator
	{
		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			throw new NotImplementedException();
		}
	}

	public class Mult : IntegerOperator
	{
		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			throw new NotImplementedException();
		}
	}

	public class Div : IntegerOperator
	{
		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			throw new NotImplementedException();
		}
	}
}