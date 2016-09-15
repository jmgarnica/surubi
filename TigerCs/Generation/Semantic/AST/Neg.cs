using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.Semantic.AST
{
	public class Neg : Expresion
	{
		IExpresion operand { get; set; }

		public override bool CheckSemantics(ISemanticChecker sc, ErrorReport report)
		{
			if (!operand.CheckSemantics(sc, report)) return false;

			TypeInfo _int = sc.Int(report);
			if (_int == null) return false;

			if (operand == null)
			{
				report.Add(
					new TigerStaticError
					{
						Column = column,
						Line = line,
						Level = ErrorLevel.Critical,
						ErrorMessage = "Null operand"
					});
				return false;
			}

			if (!operand.Return.Equals(_int))
			{
				report.Add(
					new TigerStaticError
					{
						Column = column,
						Line = line,
						Level = ErrorLevel.Error,
						ErrorMessage = "The expresion to negate must be of integer type, type provided: " + operand.Return
					});
				return false;
			}

			Return = _int;
			ReturnValue = new HolderInfo { Type = _int, Name = operand.ReturnValue.Name + "|> negation" };

			return true;
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			operand.GenerateCode(cg, report);
			if (!operand.ReturnValue.Bounded) return;

			ReturnValue.BCMMember = cg.InstrInverse_TempBound((H)operand.ReturnValue.BCMMember);
		}
	}
}
