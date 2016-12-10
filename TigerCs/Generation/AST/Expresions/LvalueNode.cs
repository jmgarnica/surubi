using System;
using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Expresions
{
	public class Var : Expresion, ILValue
	{
		public string Name { get; set; }

		public override bool CheckSemantics(ISemanticChecker sp, ErrorReport report)
		{
			if (string.IsNullOrEmpty(Name))
			{
				report.IncompleteMemberInitialization(GetType().Name);
				return false;
			}

			MemberInfo var;
			if (!sp.Reachable(Name, out var))
			{
				report.Add(new TigerStaticError { Column = column, Line = line, ErrorMessage = "Unknown symbol " + Name, Level = ErrorLevel.Error });
				return false;
			}

			if (!(var is HolderInfo))
			{
				report.Add(new TigerStaticError { Column = column, Line = line, ErrorMessage = string.Format("Member {0} is not a variable", Name), Level = ErrorLevel.Error });
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
			{
				report.Add(new TigerStaticError { Column = column, Line = line, ErrorMessage = string.Format("Member {0} not initialized", Name), Level = ErrorLevel.Critical });
			}
		}

		public void SetValue<T, F, H>(IByteCodeMachine<T, F, H> cg, H source, ErrorReport report)
			where T : class, IType<T, F>
			where F : class, IFunction<T, F>
			where H : class, IHolder
		{
			if (!ReturnValue.Bounded)
			{
				report.Add(new TigerStaticError { Column = column, Line = line, ErrorMessage = string.Format("Member {0} not initialized", Name), Level = ErrorLevel.Critical });
			}
			else cg.InstrAssing((H)Return.BCMMember, source);
		}
	}

	public class ArrayAccess : Expresion, ILValue
	{
		[Release]
		public IExpresion Array { get; set; }
		[Release]
		public IExpresion Indexer { get; set; }

		public override bool CheckSemantics(ISemanticChecker sp, ErrorReport report)
		{
			if (Array == null || Indexer == null)
			{
				report.IncompleteMemberInitialization(GetType().Name);
				return false;
			}
			if (!Array.CheckSemantics(sp, report)) return false;
			if (!Indexer.CheckSemantics(sp, report)) return false;

			if (Array.Return.ArrayOf == null)
			{
				report.Add(new TigerStaticError(line, column, "Array access to non-array type", ErrorLevel.Error));
				return false;
			}

			if (!Indexer.Return.Equals(sp.Int(report)))
			{
				report.Add(new TigerStaticError(line, column, "Array indexer must be an expresion of type 'int'", ErrorLevel.Error));
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
					report.Add(new TigerStaticError(line, column, "Index must be non-negative", ErrorLevel.Error));
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
					report.Add(new TigerStaticError(line, column, "Index must be non-negative", ErrorLevel.Error));
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