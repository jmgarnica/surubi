using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Expressions
{
	public class IntegerConstant : Expression
	{
		public IntegerConstant()
		{
			Pure = true;
		}
		int value;

		public override bool CheckSemantics(ISemanticChecker sp, ErrorReport report, TypeInfo expected = null)
		{
			if (!int.TryParse(Lex, out value))
			{
				report.Add(new StaticError(line, column, "Integer parsing error", ErrorLevel.Internal, Lex));
				return false;
			}

			Return = sp.Int(report);
			ReturnValue = new HolderInfo {Type = Return, ConstValue = value};
			return true;
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			ReturnValue.BCMMember = cg.AddConstant(value);
		}
	}

	public class StringConstant : Expression
	{
		public StringConstant()
		{
			Pure = true;
		}

		public override bool CheckSemantics(ISemanticChecker sp, ErrorReport report, TypeInfo expected = null)
		{
			//TODO: check string format
			if (Lex == null)
			{
				report.Add(new StaticError(line, column, "String constant parsing error, null lex", ErrorLevel.Internal));
				return false;
			}
			Return = sp.String(report);
			ReturnValue = new HolderInfo {Type = Return, ConstValue = Lex};
			return true;
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			ReturnValue.BCMMember = cg.AddConstant(Lex);
		}
	}

	public class NilConstant : Expression
	{
		public NilConstant()
		{
			Pure = true;
		}

		public override bool CheckSemantics(ISemanticChecker sp, ErrorReport report, TypeInfo expected = null)
		{
			Return = sp.Null(report);
			ReturnValue = sp.Nil(report);
			return true;
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			if(ReturnValue.Bounded) return;

			H nil;
			if (!cg.TryBindSTDConst("nil", out nil))
			{
				report.Add(new StaticError(line,column,$"There is no definition for nil in {cg.GetType().FullName}", ErrorLevel.Internal));
				return;
			}

			ReturnValue.BCMMember = nil;
		}
	}
}