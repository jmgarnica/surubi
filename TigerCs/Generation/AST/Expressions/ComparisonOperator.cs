using System;
using System.Collections.Generic;
using TigerCs.CompilationServices;
using TigerCs.CompilationServices.AutoCheck;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Expressions
{
	public abstract class ComparisonOperator : BinaryOperator
	{
		public FunctionInfo StringComparer { get; protected set; }

		protected TypeInfo _int, _string;
		HolderInfo _nill;

		public override bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{
			if (Right == null || Left == null)
			{
				report.Add(
					new StaticError
					{
						Column = column,
						Line = line,
						Level = ErrorLevel.Internal,
						ErrorMessage = "Null operand"
					});
				return false;
			}

			if (!Right.CheckSemantics(sc, report)) return false;
			if (!Left.CheckSemantics(sc, report)) return false;

			_int = sc.Int(report);
			if (_int == null) return false;

			_string = sc.String(report);
			if (_string == null) return false;

			if (!Right.Return.Equals(Left.Return))
			{
				report.Add(
						new StaticError
						{
							Column = column,
							Line = line,
							Level = ErrorLevel.Error,
							ErrorMessage =
								$"Comparisons must be between objects of the same type left: {Left.Return}, rigth: {Right.Return} "
						});
				return false;
			}
			else if(!(Right.Return.Equals(_int) || Right.Return.Equals(_string)))
            {
				report.Add(
					new StaticError
					{
						Column = column,
						Line = line,
						Level = ErrorLevel.Error,
						ErrorMessage = $"Type {Left.Return} not supported for comparison, only {_string} and {_int}"
					});
				return false;
			}

			Return = _int;
			ReturnValue = new HolderInfo { Type = _int, Name = Right.ReturnValue.Name + "=>?<=" + Left.ReturnValue.Name + "|> comparison" };

			if (StringComparer == null && Right.Return.Equals(_string))
			{
				_nill = sc.Nil();

				StringComparer = new FunctionInfo()
				{
					Name = "stringcompare",
					Parameters = new List<Tuple<string, TypeInfo>> { new Tuple<string, TypeInfo>("arg1", _string), new Tuple<string, TypeInfo>("arg2", _string) },
					Return = _int,
				};


			}

			return true;
		}

        public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
        {

            Right.GenerateCode(cg, report);
            if (!Right.ReturnValue.Bounded) return;
            Left.GenerateCode(cg, report);
            if (!Left.ReturnValue.Bounded) return;

        }
    }

	public class GreaterThan : ComparisonOperator
	{
		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
            base.GenerateCode(cg,report);
            H ret = cg.BindVar((T)_int.BCMMember, cg.AddConstant(0));

            var _true = cg.ReserveInstructionLabel("true");
            var _false = cg.ReserveInstructionLabel("false");
            var _end = cg.ReserveInstructionLabel("end");

            if (Left.Return.Equals(Return))
			{
				ret = cg.InstrSub_TempBound((H)Left.ReturnValue.BCMMember, (H)Right.ReturnValue.BCMMember);
            }
			else {
                cg.Call((F)StringComparer.BCMMember, new[] { (H)Left.ReturnValue.BCMMember, (H)Right.ReturnValue.BCMMember }, ret);
            }
            cg.GotoIfNotNegative(_true, ret);
            cg.Goto(_false);


            cg.ApplyReservedLabel(_true);
            cg.InstrAssing(ret, cg.AddConstant(1));
            cg.Goto(_end);

            cg.ApplyReservedLabel(_false);
            cg.InstrAssing(ret, cg.AddConstant(0));

            cg.ApplyReservedLabel(_end);
			cg.Comment(" ");

            ReturnValue.BCMMember = ret;
        }
    }

	public class GreaterEqualThan : ComparisonOperator
	{
		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
            base.GenerateCode(cg, report);
            H ret = cg.BindVar((T)_int.BCMMember, cg.AddConstant(0));

            var _true = cg.ReserveInstructionLabel("true");
            var _false = cg.ReserveInstructionLabel("false");
            var _end = cg.ReserveInstructionLabel("end");

            if (Left.Return.Equals(Return))
            {
                ret = cg.InstrSub_TempBound((H)Left.ReturnValue.BCMMember, (H)Right.ReturnValue.BCMMember);
            }
            else
            {
                cg.Call((F)StringComparer.BCMMember, new[] { (H)Left.ReturnValue.BCMMember, (H)Right.ReturnValue.BCMMember }, ret);
            }
            cg.GotoIfNotNegative(_true, ret);
            cg.GotoIfZero(_true, ret);
            cg.Goto(_false);


            cg.ApplyReservedLabel(_true);
            cg.InstrAssing(ret, cg.AddConstant(1));
            cg.Goto(_end);

            cg.ApplyReservedLabel(_false);
            cg.InstrAssing(ret, cg.AddConstant(0));

            cg.ApplyReservedLabel(_end);
            cg.Comment(" ");

            ReturnValue.BCMMember = ret;
        }
	}

	public class LessThan : ComparisonOperator
	{
		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
            base.GenerateCode(cg, report);
            H ret = cg.BindVar((T)_int.BCMMember, cg.AddConstant(0));

            var _true = cg.ReserveInstructionLabel("true");
            var _false = cg.ReserveInstructionLabel("false");
            var _end = cg.ReserveInstructionLabel("end");

            if (Left.Return.Equals(Return))
            {
                ret = cg.InstrSub_TempBound((H)Left.ReturnValue.BCMMember, (H)Right.ReturnValue.BCMMember);
            }
            else
            {
                cg.Call((F)StringComparer.BCMMember, new[] { (H)Left.ReturnValue.BCMMember, (H)Right.ReturnValue.BCMMember }, ret);
            }
            cg.GotoIfNegative(_true, ret);
            cg.Goto(_false);


            cg.ApplyReservedLabel(_true);
            cg.InstrAssing(ret, cg.AddConstant(1));
            cg.Goto(_end);

            cg.ApplyReservedLabel(_false);
            cg.InstrAssing(ret, cg.AddConstant(0));

            cg.ApplyReservedLabel(_end);
            cg.Comment(" ");

            ReturnValue.BCMMember = ret;
        }
	}

	public class LessEqualThan : ComparisonOperator
	{
		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
            base.GenerateCode(cg, report);
            H ret = cg.BindVar((T)_int.BCMMember, cg.AddConstant(0));

            var _true = cg.ReserveInstructionLabel("true");
            var _false = cg.ReserveInstructionLabel("false");
            var _end = cg.ReserveInstructionLabel("end");

            if (Left.Return.Equals(Return))
            {
                ret = cg.InstrSub_TempBound((H)Left.ReturnValue.BCMMember, (H)Right.ReturnValue.BCMMember);
            }
            else
            {
                cg.Call((F)StringComparer.BCMMember, new[] { (H)Left.ReturnValue.BCMMember, (H)Right.ReturnValue.BCMMember }, ret);
            }
            cg.GotoIfNegative(_true, ret);
            cg.GotoIfZero(_true, ret);
            cg.Goto(_false);


            cg.ApplyReservedLabel(_true);
            cg.InstrAssing(ret, cg.AddConstant(1));
            cg.Goto(_end);

            cg.ApplyReservedLabel(_false);
            cg.InstrAssing(ret, cg.AddConstant(0));

            cg.ApplyReservedLabel(_end);
            cg.Comment(" ");

            ReturnValue.BCMMember = ret;
        }
	}
}