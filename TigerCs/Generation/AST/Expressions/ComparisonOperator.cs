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
			if (Rigth == null || Left == null)//TODO: esto esta muy maja
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

			if (!Rigth.CheckSemantics(sc, report)) return false;
			if (!Left.CheckSemantics(sc, report)) return false;

			_int = sc.Int(report);
			if (_int == null) return false;

			_string = sc.String(report);
			if (_string == null) return false;

			if (!Rigth.Return.Equals(Left.Return))
			{
				report.Add(
						new StaticError
						{
							Column = column,
							Line = line,
							Level = ErrorLevel.Error,
							ErrorMessage =
								$"Comparisons must be between objects of the same type left: {Left.Return}, rigth: {Rigth.Return} "
						});
				return false;
			}
			if(!(Rigth.Return.Equals(_int) || Rigth.Return.Equals(_string)))
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
			ReturnValue = new HolderInfo { Type = _int, Name = Rigth.ReturnValue.Name + "=>?<=" + Left.ReturnValue.Name + "|> comparison" };

			if (StringComparer == null && Rigth.Return.Equals(_string))
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
			Rigth.GenerateCode(cg, report);
			if (!Rigth.ReturnValue.Bounded) return;
			Left.GenerateCode(cg, report);
			if (!Left.ReturnValue.Bounded) return;

			if (Left.Return.Equals(Return))
			{
				var val = cg.InstrSub_TempBound((H)Left.ReturnValue.BCMMember, (H)Rigth.ReturnValue.BCMMember);

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