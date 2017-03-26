using TigerCs.CompilationServices;
using TigerCs.CompilationServices.AutoCheck;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Expressions
{

	public class Assign : Expression
	{
		[NotNull]
		[SemanticChecked(CheckOrder = 1, Expected = ExpectedType.Dependent, Dependency = nameof(Source))]
		public ILValue Target { get; set; }

		[NotNull]
		[SemanticChecked]
        [ReturnType(ExpectedType.Dependent, Dependency = nameof(Target))]
		public IExpression Source { get; set; }

		public override bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{
			TypeInfo _void = sc.Void(report);
			TypeInfo _null = sc.Null(report);
			TypeInfo _string = sc.String(report);

			if (!this.AutoCheck(sc, report, expected)) return false;

			if (Source.Return == _void)
			{
				report.Add(new StaticError(Source.line, Source.column, "The given expression returns no value", ErrorLevel.Error));
				return false;
			}

			if (Source.Return == _null && Target.Return.ArrayOf == null && Target.Return.Members == null && Target.Return != _string)
			{
				report.Add(new StaticError(line, column, "Only arrays, records and strings can be nil", ErrorLevel.Error));
				return false;
			}

			if (Target.ReturnValue.Const)
			{
				report.Add(new StaticError(Target.line, Target.column, "Assignation target is constant", ErrorLevel.Error));
				return false;
			}

			Pure = false;
			Return = _void;
			return true;
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			Source.GenerateCode(cg, report);
			Target.SetValue(cg, (H)Source.ReturnValue.BCMMember, report);
		}
	}
}