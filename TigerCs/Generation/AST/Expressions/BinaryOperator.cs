using System;
using System.Collections.Generic;
using TigerCs.CompilationServices;
using TigerCs.CompilationServices.AutoCheck;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Expressions
{
	public abstract class BinaryOperator : Expression
	{
		[NotNull]
		[SemanticChecked(Expected = ExpectedType.Dependent, Dependency = nameof(Left), CheckOrder = 1)]
		[ReturnType(ExpectedType.Dependent, Dependency = nameof(Left))]
		public IExpression Right { get; set; }

		[NotNull]
		[SemanticChecked(Expected = ExpectedType.Expected)]
		public IExpression Left { get; set; }
	}

	public class EqualityOperator : BinaryOperator
	{
		public bool Equal { get; set; } = true;

		bool? comparisontype;
		FunctionInfo strcomparer;
		TypeInfo _string, _int, _void, _null;
		public override bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{
			_string = sc.String(report);
			_int = sc.Int(report);
			_void = sc.Void(report);
			_null = sc.Null(report);

			if(!this.AutoCheck(sc, report, _int))return false;

			if (Left.Return.Equals(_void))
			{
				report.Add(new StaticError(Left.line, Left.column, "Can't compare an expression that does not return a value",
										   ErrorLevel.Error));
				return false;
			}

			var notnil = Left.Return.Equals(_null) ? Right : Left;

			if (notnil.Return.Equals(_null))
			{
				report.Add(new StaticError(line, column, "The type of the comparison operands can't be inferred because both are nil",
										   ErrorLevel.Error));
				return false;
			}

			if (notnil.Return.Equals(_string))
			{
				comparisontype = true;
				MemberInfo f;
				if (!sc.Reachable(MemberInfo.MakeCompilerName("str_comparer"), out f, new MemberDefinition
				{
					line = line,
					column = column,
					Member = new FunctionInfo
					{
						Name = "strcomparer",
						Parameters =
											  new List<Tuple<string, TypeInfo>>
											  {
												  new Tuple<string, TypeInfo>("a", _string),
												  new Tuple<string, TypeInfo>("b", _string)
											  },
						Return = _int
					}
				}))
				{
					report.Add(new StaticError(Right.line, Right.column, "String comparison function missing",
										   ErrorLevel.Internal));
					return false;
				}

				strcomparer = f as FunctionInfo;
			}
			else if (notnil.Return.Equals(_int)) comparisontype = false;
			else comparisontype = null;

			Return = _int;
			ReturnValue = new HolderInfo { Type = Return };
			CanBreak = false;
			Pure = Left.Pure && Right.Pure;

			return true;
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			H ret;
			ReturnValue.BCMMember = ret = cg.BindVar((T)_int.BCMMember, cg.AddConstant(0));

			Left.GenerateCode(cg, report);
			H l = (H)Left.ReturnValue.BCMMember;
			Right.GenerateCode(cg,report);
			H r = (H)Right.ReturnValue.BCMMember;

			switch (comparisontype)
			{
				case true:
					cg.Call((F)strcomparer.BCMMember, new[] {l, r}, ret);
					break;
				case false:
					cg.InstrSub(ret, l, r);
					break;

				case null:
				default:
                    cg.InstrRefEq(ret, l, r);
					break;
			}

			if (Equal)
			{
				var _true = cg.ReserveInstructionLabel("true");
				var _false = cg.ReserveInstructionLabel("false");

				cg.GotoIfZero(_true, ret); // tiene que haber una mejor forma de hacer esto
				cg.InstrAssing(ret, cg.AddConstant(0));
				cg.Goto(_false);

				cg.ApplyReservedLabel(_true);
				cg.InstrAssing(ret, cg.AddConstant(1));

				cg.ApplyReservedLabel(_false);
				cg.Comment(" ");
			}
			else
			{
				var _false = cg.ReserveInstructionLabel("false");

				cg.GotoIfZero(_false, ret);
				cg.InstrAssing(ret, cg.AddConstant(1));
				cg.ApplyReservedLabel(_false);
				cg.Comment(" ");
			}
		}
	}
}