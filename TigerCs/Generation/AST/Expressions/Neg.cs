using TigerCs.CompilationServices;
using TigerCs.CompilationServices.AutoCheck;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Expressions
{
	public class Neg : Expression
	{
		[NotNull]
		IExpression operand { get; set; }

		public override bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{
			if (!operand.CheckSemantics(sc, report)) return false;

			TypeInfo _int = sc.Int(report);
			if (_int == null) return false;

			if (!operand.Return.Equals(_int))
			{
				report.Add(
				           new StaticError
				           {
					           Column = column,
					           Line = line,
					           Level = ErrorLevel.Error,
					           ErrorMessage = $"Can not perform (-)({operand.Return})"
				           });
				return false;
			}

			Return = _int;
			ReturnValue = new HolderInfo { Type = _int };

			if (operand.ReturnValue.ConstValue != null)
				ReturnValue.ConstValue = -(int)operand.ReturnValue.ConstValue;
			return true;
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			if (ReturnValue.ConstValue != null)
			{
				ReturnValue.BCMMember = cg.AddConstant((int)ReturnValue.ConstValue);
				return;
			}

			operand.GenerateCode(cg, report);
			ReturnValue.BCMMember = cg.InstrInverse_TempBound((H)operand.ReturnValue.BCMMember);
		}
	}
}
