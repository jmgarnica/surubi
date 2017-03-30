using TigerCs.CompilationServices;
using TigerCs.CompilationServices.AutoCheck;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Expressions
{
	public class Neg : Expression
	{
		[NotNull]
		public IExpression Operand { get; set; }

		public override bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{
			if (!Operand.CheckSemantics(sc, report)) return false;

			TypeInfo _int = sc.Int(report);
			if (_int == null) return false;

			if (!Operand.Return.Equals(_int))
			{
				report.Add(
				           new StaticError
				           {
					           Column = column,
					           Line = line,
					           Level = ErrorLevel.Error,
					           ErrorMessage = $"Can not perform (-)({Operand.Return})"
				           });
				return false;
			}

			Return = _int;
			ReturnValue = new HolderInfo { Type = _int };

			if (Operand.ReturnValue.ConstValue != null)
				ReturnValue.ConstValue = -(int)Operand.ReturnValue.ConstValue;
			return true;
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			if (ReturnValue.ConstValue != null)
			{
				ReturnValue.BCMMember = cg.AddConstant((int)ReturnValue.ConstValue);
				return;
			}

			Operand.GenerateCode(cg, report);
			ReturnValue.BCMMember = cg.InstrInverse_TempBound((H)Operand.ReturnValue.BCMMember);
		}
	}
}
