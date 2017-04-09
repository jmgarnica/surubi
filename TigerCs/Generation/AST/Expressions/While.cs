using TigerCs.CompilationServices;
using TigerCs.CompilationServices.AutoCheck;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Expressions
{
	public class While : Expression
	{
		//TODO: poner lo demas
		[ReturnType(ExpectedType.Int)]
		public IExpression Condition { get; set; }

		[NotNull]
		[ReturnType(ExpectedType.Void)]
		public IExpression Body { get; set; }

		LoopScopeDescriptor end;

		public override bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{
			Return = sc.Void(report);
			ReturnValue = null;
			Pure = (Condition?.Pure ?? true) && Body.Pure;
			CanBreak = Condition?.CanBreak ?? false;

			end = new LoopScopeDescriptor();

			var _int = sc.Int();
			sc.EnterNestedScope(descriptors: end);

			if (Condition != null)
			{
				if (!Condition.CheckSemantics(sc, report, _int))
				{
					sc.LeaveScope();
					return false;
				}

				if (!Condition.Return.Equals(_int))
				{
					report.Add(new StaticError(line,column,$"Loop condition must be of type {_int}, but {Condition.Return} was given.",ErrorLevel.Error));
					sc.LeaveScope();
					return false;
				}
			}

			if (!Body.CheckSemantics(sc, report, Return))
			{
				sc.LeaveScope();
				return false;
			}

			if (!Body.Return.Equals(Return))
			{
				report.Add(new StaticError(line, column, "Loop body can not return a value.", ErrorLevel.Error));
				sc.LeaveScope();
				return false;
			}

			sc.LeaveScope();

			if ((Condition == null || (Condition?.ReturnValue.ConstValue != null && (int)Condition.ReturnValue.ConstValue > 0)) &&
			    !Body.CanBreak)
				report.Add(new StaticError(line, column, "Infinite loop", ErrorLevel.Warning));

			if (Condition?.ReturnValue.ConstValue != null && (int)Condition.ReturnValue.ConstValue == 0)
				report.Add(new StaticError(Body.line, Body.column, "Unreachable code detected: Condition always false", ErrorLevel.Warning));

			return true;
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			if (Condition == null || (Condition.ReturnValue.ConstValue != null && (int)Condition.ReturnValue.ConstValue != 0))
			{
				cg.EnterNestedScope();
				end.ENDLabel = cg.EndScope;

				if(Condition?.Pure == false) Condition.GenerateCode(cg,report);
				Body.GenerateCode(cg, report);
				cg.Goto(cg.BiginScope);

				cg.LeaveScope();
			}
			else if(Condition.ReturnValue.ConstValue == null) // excludes Condition.ReturnValue.ConstValue = 0 case
			{
				cg.EnterNestedScope();
				end.ENDLabel = cg.EndScope;

				Condition.GenerateCode(cg, report);
				cg.GotoIfZero(cg.EndScope, (H)Condition.ReturnValue.BCMMember);

				Body.GenerateCode(cg, report);
				cg.Goto(cg.BiginScope);

				cg.LeaveScope();
			}
		}
	}
}