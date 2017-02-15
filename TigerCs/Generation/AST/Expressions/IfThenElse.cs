using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Expressions
{
	public class IfThenElse : Expression
	{
		[Release]
		[NotNull]
		public IExpression If { get; set; }

		[Release]
		[NotNull]
		public IExpression Then { get; set; }

		[Release]
		public IExpression Else { get; set; }

		bool? alwaystakethen;
		HolderInfo nil;
		TypeInfo Null;

		public override bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{
			var _int = sc.Int(report);
			var _void = sc.Void(report);
			Null = sc.Null(report);
			nil = sc.Nil(report);

			alwaystakethen = null;

			if (!If.CheckSemantics(sc, report, _int) ) return  false;

			if (If.Return != _int)
			{
				report.Add(new StaticError(If.line, If.column,
										   $"The type of given condition is {If.Return}" +
										   $" where it should be {_int}", ErrorLevel.Error));
				return false;
			}

			sc.EnterNestedScope();
			if (!Then.CheckSemantics(sc, report, expected))
			{
				sc.LeaveScope();
				return false;
			}
			sc.LeaveScope();

			if (Else != null)
			{
				sc.EnterNestedScope();
				if (!Else.CheckSemantics(sc, report, Then.Return))
				{
					sc.LeaveScope();
					return false;
				}
				sc.LeaveScope();
			}

			if (Else != null)
			{
				if (Then.Return != Else.Return)
				{

					if (Then.Return == _int || Else.Return == _int || (Then.Return != Null && Else.Return != Null))
					{
						report.Add(new StaticError(line, column, $"Then-expression[{Then.Return}] and " +
						                                         $"Else-expression[{Else.Return}] must have the same return" +
						                                         " type or do not return any value", ErrorLevel.Error));
						return false;
					}
					Return = Then.Return != Null? Then.Return : Else.Return;
				}
				else Return = Then.Return;
			}
			else if (Then.Return != _void)
			{
				report.Add(new StaticError(Then.line, Then.column, "If-Then expression can not return a value",
				                           ErrorLevel.Error));
				return false;
			}
			else Return = _void;

			Pure = If.Pure && Then.Pure && (Else?.Pure ?? true);
			ReturnValue = Return != _void? new HolderInfo {Type = Return} : null;

			#region Interpretation

			if (If.ReturnValue.ConstValue != null)
			{
				alwaystakethen = (int)If.ReturnValue.ConstValue != 0;

				if (ReturnValue == null) return true;

				if (alwaystakethen.Value)
				{
					if (Else != null)
						report.Add(new StaticError(Else.line, Else.column,
						                           "Unreachable code detected: Constant condition", ErrorLevel.Warning));
					else
						report.Add(new StaticError(If.line, If.column, "Redundant condition",
													   ErrorLevel.Warning));

					if (ReturnValue != null && Then.ReturnValue.ConstValue != null)
						ReturnValue.ConstValue = Then.ReturnValue.ConstValue;
				}
				else
				{
					report.Add(new StaticError(Then.line, Then.column,
												   "Unreachable code detected: Constant condition", ErrorLevel.Warning));

					if (ReturnValue != null && Else?.ReturnValue.ConstValue != null)
						ReturnValue.ConstValue = Else.ReturnValue.ConstValue;
				}
			}
			else if (ReturnValue != null && Then.ReturnValue?.ConstValue != null && Else?.ReturnValue.ConstValue != null)
			{
				if (Then.ReturnValue.ConstValue.Equals(Else.ReturnValue.ConstValue))
					ReturnValue.ConstValue = Then.ReturnValue.ConstValue;
			}

			#endregion

			return true;
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			IExpression always;
			if (alwaystakethen != null && (always = alwaystakethen.Value? Then : Else) != null)
			{
				if (!If.Pure) If.GenerateCode(cg, report);

				H mmb;
				if (ReturnValue?.ConstValue != null)
				{
					if (ReturnValue.ConstValue.GenerateBCMMember(cg, out mmb))
					{
						ReturnValue.BCMMember = mmb;
						if (always.Pure) return;

						cg.EnterNestedScope();
						always.GenerateCode(cg, report);
						cg.LeaveScope();
					}
					else
					{
						mmb = cg.BindVar((T)Return?.BCMMember);
						if (If.Pure) If.GenerateCode(cg, report);

						cg.EnterNestedScope();
						always.GenerateCode(cg, report);
						if (ReturnValue != null)
							cg.InstrAssing(mmb, (H)always.ReturnValue.BCMMember);
						cg.LeaveScope();

						if (ReturnValue != null)
							ReturnValue.BCMMember = mmb;
					}
				}
				else
				{
					mmb = ReturnValue == null? null : cg.BindVar((T)Return?.BCMMember);

					cg.EnterNestedScope();
					always.GenerateCode(cg, report);
					if (ReturnValue != null)
						cg.InstrAssing(mmb, (H)always.ReturnValue.BCMMember);
					cg.LeaveScope();

					if (ReturnValue != null)
						ReturnValue.BCMMember = mmb;
				}
			}
			else
			{
				var skipelse = cg.ReserveInstructionLabel("then");
				var skipthen = cg.ReserveInstructionLabel("else/skip");
				H result = default(H);

				if (ReturnValue != null)
					result = Return == Null? (H)nil.BCMMember : cg.BindVar((T)Return.BCMMember);

				If.GenerateCode(cg, report);
				cg.GotoIfZero(skipthen, (H)If.ReturnValue.BCMMember);

				cg.EnterNestedScope(namehint: "then");
				Then.GenerateCode(cg, report);
				if (ReturnValue != null) cg.InstrAssing(result, (H)Then.ReturnValue.BCMMember);
				cg.LeaveScope();

				if (Else != null)cg.Goto(skipelse);
				cg.ApplyReservedLabel(skipthen);

				if (Else != null)
				{
					cg.EnterNestedScope(namehint: "else");
					Else.GenerateCode(cg, report);
					if (ReturnValue != null) cg.InstrAssing(result, (H)Else.ReturnValue.BCMMember);
					cg.LeaveScope();

					cg.ApplyReservedLabel(skipelse);
				}

				if (ReturnValue != null) ReturnValue.BCMMember = result;
			}
		}
	}
}