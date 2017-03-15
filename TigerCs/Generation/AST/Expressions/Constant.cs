using TigerCs.CompilationServices;
using TigerCs.CompilationServices.AutoCheck;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Expressions
{
	public class IntegerConstant : Expression
	{
		[NotNull("")]
		public override string Lex { get; set; }

		public IntegerConstant()
		{
			Pure = true;
		}
		int value;

		public override bool CheckSemantics(ISemanticChecker sp, ErrorReport report, TypeInfo expected = null)
		{
			if (this.AutoCheck(sp, report, expected) && !int.TryParse(Lex, out value))
			{
				report.Add(new StaticError(line, column, "Integer parsing error", ErrorLevel.Internal, Lex));
				//TODO: there is no need ¿for/of? stopping the semantic check, but the checking must fail at the end
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
		[NotNull]
		public override string Lex { get; set; }

		public StringConstant()
		{
			Pure = true;
		}

		public override bool CheckSemantics(ISemanticChecker sp, ErrorReport report, TypeInfo expected = null)
		{
			this.AutoCheck(sp, report, expected);
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