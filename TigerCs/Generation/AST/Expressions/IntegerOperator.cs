using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Expressions
{
	public class IntegerOperator : BinaryOperator
	{
		public IntegerOp Optype { get; set; }

		TypeInfo _int;

		public override bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{
			_int = sc.Int(report);

			if (!Left.CheckSemantics(sc, report, _int) || !Rigth.CheckSemantics(sc, report, _int)) return false;

			if (Left.Return != _int || Rigth.Return != _int)
			{
				report.Add(new StaticError(line, column,
					$"Can not perform ({Optype})({Left.Return}, {Rigth.Return})",ErrorLevel.Error));
				return false;
			}

			if (Optype != IntegerOp.Addition &&
				Optype != IntegerOp.Division &&
				Optype != IntegerOp.Multiplication &&
			    Optype != IntegerOp.Subtraction)
			{
				report.Add(new StaticError(line, column, $"Unkown operator ({Optype})", ErrorLevel.Error));
				return false;
			}

			Return = _int;
			ReturnValue = new HolderInfo {Type = _int};
			Pure = Left.Pure && Rigth.Pure;

			if (Left.ReturnValue.ConstValue == null || Rigth.ReturnValue.ConstValue == null) return true;

			int rigth = (int)Rigth.ReturnValue.ConstValue;

			if (Optype == IntegerOp.Addition)
				ReturnValue.ConstValue = (int)Left.ReturnValue.ConstValue + rigth;

			if (Optype == IntegerOp.Subtraction)
				ReturnValue.ConstValue = (int)Left.ReturnValue.ConstValue - rigth;

			if (Optype == IntegerOp.Multiplication)
				ReturnValue.ConstValue = (int)Left.ReturnValue.ConstValue * rigth;

			if (Optype == IntegerOp.Division)
			{
				if (rigth == 0)
				{
					report.Add(new StaticError(Rigth.line, Rigth.column, "Division by zero", ErrorLevel.Error));
					return false;
				}
				ReturnValue.ConstValue = (int)Left.ReturnValue.ConstValue / rigth;
			}

			return true;
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			if (ReturnValue.ConstValue != null)
			{
				if (!Left.Pure) Left.GenerateCode(cg, report);
				if (!Rigth.Pure) Rigth.GenerateCode(cg, report);

				ReturnValue.BCMMember = cg.AddConstant((int)ReturnValue.ConstValue);
				return;
			}

			Left.GenerateCode(cg, report);
			Rigth.GenerateCode(cg, report);
			H rigth = (H)Rigth.ReturnValue.BCMMember;

			if (Optype == IntegerOp.Addition)
				ReturnValue.BCMMember = cg.InstrAdd_TempBound((H)Left.ReturnValue.BCMMember, rigth);

			if (Optype == IntegerOp.Subtraction)
				ReturnValue.BCMMember = cg.InstrSub_TempBound((H)Left.ReturnValue.BCMMember, rigth);

			if (Optype == IntegerOp.Multiplication)
				ReturnValue.BCMMember = cg.InstrMult_TempBound((H)Left.ReturnValue.BCMMember, rigth);

			if (Optype == IntegerOp.Division)
			{
				var zero = cg.ReserveInstructionLabel("by zero division check fail");
				var pass = cg.ReserveInstructionLabel("by zero division check success");

				cg.GotoIfZero(zero, rigth);
				ReturnValue.BCMMember = cg.InstrDiv_TempBound((H)Left.ReturnValue.BCMMember, rigth);
				cg.Goto(pass);

				cg.ApplyReservedLabel(zero);
				cg.EmitError(5, "Division by zero");
				cg.ApplyReservedLabel(pass);
				cg.BlankLine();

			}
		}
	}

	public class IntegerOp
	{
		public static readonly IntegerOp Addition;
		public static readonly IntegerOp Subtraction;
		public static readonly IntegerOp Multiplication;
		public static readonly IntegerOp Division;

		readonly char op;

		public IntegerOp(char op)
		{
			this.op = op;
		}

		static IntegerOp()
		{
			Addition = new IntegerOp('+');
			Subtraction = new IntegerOp('-');
			Multiplication = new IntegerOp('*');
			Division = new IntegerOp('/');
		}

		public override int GetHashCode()
		{
			return op.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			var io = obj as IntegerOp;
			return op == io?.op;
		}

		public override string ToString()
		{
			return op.ToString();
		}

		public static bool operator ==(IntegerOp a, IntegerOp b)
		{
			return a?.op == b?.op;
		}

		public static bool operator !=(IntegerOp a, IntegerOp b)
		{
			return a?.op != b?.op;
		}
	}
}