using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Expressions
{
	public class BoundedFor : Expression
	{
		public const int RepThreshold = 3;

		public string VarName { get; set; }

		[Release]
		[NotNull]
		public IExpression From { get; set; }

		[Release]
		[NotNull]
		public IExpression To { get; set; }

		[Release]
		[NotNull]
		public IExpression Body { get; set; }

		HolderInfo Var;
		LoopScopeDescriptor end;
		TypeInfo _int;
		HolderInfo nil;

		public override bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{
			if (string.IsNullOrEmpty(VarName))
			{
				report.IncompleteMemberInitialization(GetType().Name, line, column);
				return false;
			}

			_int = sc.Int(report);
			nil = sc.Nil(report);
			var _void = sc.Void(report);

			if (!From.CheckSemantics(sc, report, _int) || !To.CheckSemantics(sc, report, _int))
				return false;

			if (!From.Return.Equals(_int) || !To.Return.Equals(_int))
			{
				report.Add(new StaticError(line, column, "Boundaries must be interger Expressions", ErrorLevel.Error));
				return false;
			}

			end = new LoopScopeDescriptor();

			sc.EnterNestedScope(descriptors: end);
			Var = new HolderInfo {Const = true, Name = VarName, Type = _int};
			sc.DeclareMember(VarName, new MemberDefinition {column = column, line = line, Member = Var});

			if (!Body.CheckSemantics(sc, report))
			{
				sc.LeaveScope();
				return false;
			}

			if (Body.CanBreak && !Body.Return.Equals(_void))
			{
				report.Add(new StaticError(line,column, "Expressions that can break should not return", ErrorLevel.Internal));
				return false;
			}
			sc.LeaveScope();

			Return = Body.Return;
			ReturnValue = Return.Equals(_void)
				              ? null
				              : new HolderInfo {Type = Body.Return, ConstValue = Body.ReturnValue.ConstValue};
			CanBreak = From.CanBreak || To.CanBreak;
			Pure = From.Pure && To.Pure && Body.Pure;

			if (From.ReturnValue.ConstValue == null || To.ReturnValue.ConstValue == null) return true;

			int f = (int)From.ReturnValue.ConstValue;
			int t = (int)To.ReturnValue.ConstValue;
			if (t < f)
				report.Add(new StaticError(line, column, "Bounded loop over an empty range", ErrorLevel.Warning));

			if (Body.ReturnValue?.ConstValue == null || Body.CanBreak || ReturnValue == null) return true;

			ReturnValue.ConstValue = Body.ReturnValue.ConstValue;

			return true;
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			cg.Comment("Bounded For");
			H ret = ReturnValue != null
				        ? cg.BindVar((T)Return.BCMMember, Return.Equals(_int)? cg.AddConstant(0) : (H)nil.BCMMember)
				        : null;

			H f = cg.BindVar((T)_int.BCMMember);
			H finit;
			if (From.ReturnValue.ConstValue != null)
			{
				if (!From.Pure) From.GenerateCode(cg, report);
				From.ReturnValue.ConstValue.GenerateBCMMember(cg, out finit);
			}
			else
			{
				From.GenerateCode(cg, report);
				finit = (H)From.ReturnValue.BCMMember;
			}

			cg.InstrAssing(f, finit);
			cg.Release(finit);

			H t;
			if (To.ReturnValue.ConstValue != null)
			{
				if (!To.Pure) To.GenerateCode(cg, report);
				To.ReturnValue.ConstValue.GenerateBCMMember(cg, out t);
			}
			else
			{
				To.GenerateCode(cg, report);
				t = (H)To.ReturnValue.BCMMember;
			}

			Var.BCMMember = f;

			cg.EnterNestedScope();
			end.ENDLabel = cg.EndScope;

			var cmp = cg.BindVar((T)_int.BCMMember, name: "cmp");

			cg.InstrSub(cmp, t, f);
			cg.GotoIfNegative(cg.EndScope, cmp);

			Body.GenerateCode(cg, report);
			if (ReturnValue != null)
				cg.InstrAssing(ret, (H)Body.ReturnValue.BCMMember);

			cg.InstrAdd(f, f, cg.AddConstant(1));
			cg.Goto(cg.BiginScope);

			cg.LeaveScope();

			cg.Release(t);
			cg.Release(f);
		}
	}
}