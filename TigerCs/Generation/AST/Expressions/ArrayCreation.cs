using TigerCs.CompilationServices;
using TigerCs.CompilationServices.AutoCheck;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Expressions
{
	public class ArrayCreation : Expression
	{
		[NotNull]
		[SemanticChecked(Expected = ExpectedType.Int)]
		[ReturnType(ExpectedType.Int, Action = OnError.ErrorButNotStop)]
		public IExpression Length { get; set; }

		[SemanticChecked(CheckOrder = 1)]
		[ReturnType(ExpectedType.MemberOfDependent, Dependency = nameof(Return), Action = OnError.ErrorButNotStop)]
		public IExpression Init { get; set; }

		[NotNull("")]
		public string ArrayOf { get; set; }

		TypeInfo _int;
		HolderInfo nil;
		public override bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{
			if (string.IsNullOrWhiteSpace(ArrayOf))
			{
				report.IncompleteMemberInitialization(GetType().Name);
				return false;
			}

			MemberInfo mem;
			if (!sc.Reachable(TypeInfo.MakeTypeName(ArrayOf), out mem))
			{
				report.Add(new StaticError(line, column, $"Type {ArrayOf} is unaccessible", ErrorLevel.Error));
				return false;
			}

			var t = mem as TypeInfo;
			if (t == null)
			{
				report.Add(new StaticError(line, column, $"The non-type member {mem.Name} was declared in a type namespace", ErrorLevel.Internal));
				return false;
			}

			if (t.ArrayOf == null)
			{
				report.Add(new StaticError(line, column, $"{t} is not an array type", ErrorLevel.Error));
				return false;
			}

			Return = t;

			_int = sc.Int(report);
			nil = sc.Nil(report);

			if (Init == null)
				if(t.ArrayOf.Equals(_int))
					Init = new IntegerConstant
					{
						line = line,
						column = column,
						Lex = "0"
					};

				else Init = new NilConstant
				{
					line = line,
					column = column,
					Lex = "nil"
				};

			if(!this.AutoCheck(sc, report, expected)) return false;

			Pure = false;
			CanBreak = false; // no length neither init expressions can contain a break, so they would'n return
			ReturnValue = new HolderInfo
			{
				Name = Return.Name,
				Type = Return
			};

			return true;
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			T type;
			if (!Return.Bounded)
				Return.BCMMember = type = cg.BindArrayType(Return.Name, (T)Return.ArrayOf.BCMMember);
			else
				type = (T)Return.BCMMember;

			H array;//TODO: cosnt init
			ReturnValue.BCMMember = array = cg.BindVar(type);

			Length.GenerateCode(cg, report);
			H length = (H)Length.ReturnValue.BCMMember;
			cg.Call(type.Allocator, new[] { length, Return.ArrayOf.Equals(_int) ? cg.AddConstant(0) : (H)nil.BCMMember }, array);

			H cmp = cg.BindVar((T)_int.BCMMember);
			H i = cg.BindVar((T)_int.BCMMember, cg.AddConstant(0));

			var loop = cg.SetLabelToNextInstruction("LOOP");
			cg.InstrSub(cmp, length, i);

			var end = cg.ReserveInstructionLabel("END");
			cg.GotoIfZero(end, cmp);

			Init.GenerateCode(cg, report);
			cg.Call(type.DynamicMemberWriteAccess, new[] {array, i, (H)Init.ReturnValue.BCMMember});
			cg.InstrAdd(i, i, cg.AddConstant(1));

			cg.Goto(loop);
			cg.ApplyReservedLabel(end);
		}
	}
}
