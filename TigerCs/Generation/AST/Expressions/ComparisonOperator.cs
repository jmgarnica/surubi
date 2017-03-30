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

		TypeInfo _int, _string;
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
	}

	public class GreaterThan : ComparisonOperator
	{
		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			Right.GenerateCode(cg, report);
			if (!Right.ReturnValue.Bounded) return;
			Left.GenerateCode(cg, report);
			if (!Left.ReturnValue.Bounded) return;

			if (Left.Return.Equals(Return))
			{
				var val = cg.InstrSub_TempBound((H)Left.ReturnValue.BCMMember, (H)Right.ReturnValue.BCMMember);

			}
			else { }
		}
	}

	public class GreaterEqualThan : ComparisonOperator
	{
		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			throw new NotImplementedException();
		}
	}

	public class LessThan : ComparisonOperator
	{
		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			throw new NotImplementedException();
		}
	}

	public class LessEqualThan : ComparisonOperator
	{
		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			throw new NotImplementedException();
		}
	}
}