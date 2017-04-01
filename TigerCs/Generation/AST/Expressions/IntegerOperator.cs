using TigerCs.CompilationServices;
using TigerCs.CompilationServices.AutoCheck;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Expressions
{
	public class IntegerOperator : BinaryOperator
	{
		public IntegerOp Optype { get; set; }

		TypeInfo _int;

		public override bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{
			if (!this.AutoCheck(sc, report, expected)) return false;
			_int = sc.Int(report);

			if (!Left.CheckSemantics(sc, report, _int) || !Right.CheckSemantics(sc, report, _int)) return false;

			if (Left.Return != _int || Right.Return != _int)
			{
				report.Add(new StaticError(line, column,
					$"Can not perform ({Optype})({Left.Return}, {Right.Return})",ErrorLevel.Error));
				return false;
			}

			if (Optype != IntegerOp.Addition &&
				Optype != IntegerOp.Division &&
				Optype != IntegerOp.Multiplication &&
			    Optype != IntegerOp.Subtraction &&
				Optype != IntegerOp.And &&
				Optype != IntegerOp.Or)
			{
				report.Add(new StaticError(line, column, $"Unkown operator ({Optype})", ErrorLevel.Error));
				return false;
			}

			Return = _int;
			ReturnValue = new HolderInfo {Type = _int};
			Pure = Left.Pure && Right.Pure;

			if (Left.ReturnValue.ConstValue == null || Right.ReturnValue.ConstValue == null) return true;

			int rigth = (int)Right.ReturnValue.ConstValue;

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
					report.Add(new StaticError(Right.line, Right.column, "Division by zero", ErrorLevel.Error));
					return false;
				}
				ReturnValue.ConstValue = (int)Left.ReturnValue.ConstValue / rigth;
			}

			if (Optype == IntegerOp.And)
				if ((int)Left.ReturnValue.ConstValue == 0)
					ReturnValue.ConstValue = 0;
				else if (rigth == 0) ReturnValue.ConstValue = 0;
				else ReturnValue.ConstValue = 1;

			if (Optype == IntegerOp.Or)
				if ((int)Left.ReturnValue.ConstValue != 0)
					ReturnValue.ConstValue = 1;
				else if (rigth != 0) ReturnValue.ConstValue = 1;
				else ReturnValue.ConstValue = 0;

			return true;
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			if (ReturnValue.ConstValue != null)
			{
				if (!Left.Pure) Left.GenerateCode(cg, report);
				if (!Right.Pure) Right.GenerateCode(cg, report);

				ReturnValue.BCMMember = cg.AddConstant((int)ReturnValue.ConstValue);
				return;
			}

			if (Optype == IntegerOp.Addition)
			{
				Left.GenerateCode(cg, report);
				Right.GenerateCode(cg, report);
				H rigth = (H)Right.ReturnValue.BCMMember;
				ReturnValue.BCMMember = cg.InstrAdd_TempBound((H)Left.ReturnValue.BCMMember, rigth);
			}

			if (Optype == IntegerOp.Subtraction)
			{
				Left.GenerateCode(cg, report);
				Right.GenerateCode(cg, report);
				H rigth = (H)Right.ReturnValue.BCMMember;
				ReturnValue.BCMMember = cg.InstrSub_TempBound((H)Left.ReturnValue.BCMMember, rigth);
			}

			if (Optype == IntegerOp.Multiplication)
			{
				Left.GenerateCode(cg, report);
				Right.GenerateCode(cg, report);
				H rigth = (H)Right.ReturnValue.BCMMember;
				ReturnValue.BCMMember = cg.InstrMult_TempBound((H)Left.ReturnValue.BCMMember, rigth);
			}

			if (Optype == IntegerOp.Division)
			{
				Left.GenerateCode(cg, report);
				Right.GenerateCode(cg, report);
				H rigth = (H)Right.ReturnValue.BCMMember;

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

			if (Optype == IntegerOp.And)
			{
				var zero = cg.ReserveInstructionLabel("by zero division check fail");
				var pass = cg.ReserveInstructionLabel("by zero division check success");

				H r;
				ReturnValue.BCMMember = r = cg.BindVar((T)_int.BCMMember);

				Left.GenerateCode(cg, report);
				cg.GotoIfZero(zero, (H)Left.ReturnValue.BCMMember);

				Right.GenerateCode(cg, report);
				cg.GotoIfZero(zero, (H)Right.ReturnValue.BCMMember);

                cg.InstrAssing(r, cg.AddConstant(1));
				cg.Goto(pass);

				cg.ApplyReservedLabel(zero);
				cg.InstrAssing(r, cg.AddConstant(0));

				cg.ApplyReservedLabel(pass);
			}

			if (Optype == IntegerOp.Or)
			{
				var one = cg.ReserveInstructionLabel("by zero division check fail");
				var pass = cg.ReserveInstructionLabel("by zero division check success");

				H r;
				ReturnValue.BCMMember = r = cg.BindVar((T)_int.BCMMember);

				Left.GenerateCode(cg, report);
				cg.GotoIfNotZero(one, (H)Left.ReturnValue.BCMMember);

				Right.GenerateCode(cg, report);
				cg.GotoIfNotZero(one, (H)Right.ReturnValue.BCMMember);

				cg.InstrAssing(r, cg.AddConstant(0));
				cg.Goto(pass);

				cg.ApplyReservedLabel(one);
				cg.InstrAssing(r, cg.AddConstant(1));

				cg.ApplyReservedLabel(pass);
			}
		}
	}

	public class IntegerOp
	{
		public static readonly IntegerOp Addition;
		public static readonly IntegerOp Subtraction;
		public static readonly IntegerOp Multiplication;
		public static readonly IntegerOp Division;
		public static readonly IntegerOp And;
		public static readonly IntegerOp Or;

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
			And = new IntegerOp('&');
			Or = new IntegerOp('|');
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