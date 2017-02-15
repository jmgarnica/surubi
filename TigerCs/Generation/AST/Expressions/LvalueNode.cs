using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Expressions
{
	public class Var : Expression, ILValue
	{
		public string Name { get; set; }

		public override bool CheckSemantics(ISemanticChecker sp, ErrorReport report, TypeInfo expected = null)
		{
			if (string.IsNullOrEmpty(Name))
			{
				report.IncompleteMemberInitialization(GetType().Name, line, column);
				return false;
			}

			MemberInfo var;
			if (!sp.Reachable(Name, out var))
			{
				report.Add(new StaticError { Column = column, Line = line, ErrorMessage = "Unknown symbol " + Name, Level = ErrorLevel.Error });
				return false;
			}

			if (!(var is HolderInfo))
			{
				report.Add(new StaticError { Column = column, Line = line, ErrorMessage = $"Member {Name} is not a variable", Level = ErrorLevel.Error });
				return false;
			}

			var hvar = (HolderInfo)var;

			Return = hvar.Type;
			ReturnValue = hvar;
			return true;
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			if (!ReturnValue.Bounded)
				report.Add(new StaticError { Column = column, Line = line, ErrorMessage = $"Member {Name} not initialized", Level = ErrorLevel.Internal });
		}

		public void SetValue<T, F, H>(IByteCodeMachine<T, F, H> cg, H source, ErrorReport report)
			where T : class, IType<T, F>
			where F : class, IFunction<T, F>
			where H : class, IHolder
		{
			if (!ReturnValue.Bounded)
			{
				report.Add(new StaticError
				           {
					           Column = column,
					           Line = line,
					           ErrorMessage = $"Member {Name} not initialized",
					           Level = ErrorLevel.Internal
				           });
				return;
			}

			cg.InstrAssing((H)ReturnValue.BCMMember, source);
		}
	}

	public class ArrayAccess : Expression, ILValue
	{
		[Release]
		[NotNull]
		public IExpression Array { get; set; }
		[Release]
		[NotNull]
		public IExpression Indexer { get; set; }

		public override bool CheckSemantics(ISemanticChecker sp, ErrorReport report, TypeInfo expected = null)
		{
			if (!Array.CheckSemantics(sp, report)) return false;
			if (!Indexer.CheckSemantics(sp, report)) return false;

			if (Array.Return.ArrayOf == null)
			{
				report.Add(new StaticError(line, column, "Array access to non-array type", ErrorLevel.Error));
				return false;
			}

			var _int = sp.Int(report);
            if (!Indexer.Return.Equals(_int))
			{
				report.Add(new StaticError(line, column, $"Array indexer must be an expression of type {_int}", ErrorLevel.Error));
				return false;
			}

			Return = Array.Return.ArrayOf;
			ReturnValue = new HolderInfo { Type = Return, Name = "Array Index" };
			return true;
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			Array.GenerateCode(cg, report);
			Indexer.GenerateCode(cg, report);
			if (Indexer.ReturnValue.ConstValue != null)
			{
				int index = (int)Indexer.ReturnValue.ConstValue;
				if (index < 0)
				{
					report.Add(new StaticError(line, column, "Index must be non-negative", ErrorLevel.Error));
					return;
				}
				ReturnValue.BCMMember = cg.StaticMemberAcces((T)Array.Return.BCMMember, (H)Array.ReturnValue.BCMMember, index);
			}
			else
			{
				var ret = cg.BindVar((T)Array.Return.ArrayOf.BCMMember);
				cg.Call(((T)Array.Return.BCMMember).DynamicMemberReadAccess, new[] { (H)Array.ReturnValue.BCMMember, (H)Indexer.ReturnValue.BCMMember }, ret);
				ReturnValue.BCMMember = ret;
			}
		}

		public void SetValue<T, F, H>(IByteCodeMachine<T, F, H> cg, H source, ErrorReport report)
			where T : class, IType<T, F>
			where F : class, IFunction<T, F>
			where H : class, IHolder
		{
			Array.GenerateCode(cg, report);
			Indexer.GenerateCode(cg, report);
			if (Indexer.ReturnValue.ConstValue != null)
			{
				int index = (int)Indexer.ReturnValue.ConstValue;
				if (index < 0)
				{
					report.Add(new StaticError(line, column, "Index must be non-negative", ErrorLevel.Error));
					return;
				}
				var sma = cg.StaticMemberAcces((T)Array.Return.BCMMember, (H)Array.ReturnValue.BCMMember, index);
				cg.InstrAssing(sma, source);
			}
			else
			{
				cg.Call(((T)Array.Return.BCMMember).DynamicMemberWriteAccess, new[] { (H)Array.ReturnValue.BCMMember, (H)Indexer.ReturnValue.BCMMember, source });
			}
		}
	}
}