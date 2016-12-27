using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Expresions
{

	public class Assign : Expresion
	{
		[NotNull]
		[Release]
		public ILValue Target { get; set; }

		[NotNull]
		[Release]
		public IExpresion Source { get; set; }

		public override bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{
			TypeInfo _void = sc.Void(report);
			TypeInfo _null = sc.Null(report);
			TypeInfo _string = sc.String(report);

			if (!Source.CheckSemantics(sc, report)) return false;

			if (Source.Return == _void)
			{
				report.Add(new StaticError(Source.line, Source.column, "The given expresion returns no value", ErrorLevel.Error));
				return false;
			}

			if (!Target.CheckSemantics(sc, report, Source.Return)) return false;

			if (Source.Return == _null && Target.Return.ArrayOf == null && Target.Return.Members == null && Target.Return != _string)
			{
				report.Add(new StaticError(line, column, "Only arrays, records and strings can be nil", ErrorLevel.Error));
				return false;
			}

			if (Target.Return != Source.Return)
			{
				report.Add(new StaticError(line, column, $"Assign between incompatible types: {Target.Return}, {Source.Return}",
				                           ErrorLevel.Error));
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