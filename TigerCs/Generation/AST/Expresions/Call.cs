using System;
using System.Collections.Generic;
using System.Linq;
using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Expresions
{
	public class Call : Expresion
	{
		public string FunctionName { get; set; }

		[Release(true)]
		public List<IExpresion> Arguments { get; set; }

		FunctionInfo func;

		public override bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{
			if (string.IsNullOrEmpty(FunctionName))
			{
				report.IncompleteMemberInitialization(GetType().Name, line, column);
				return false;
			}

			var _string = sc.String(report);
			var _null = sc.Null(report);
			var _void = sc.Void(report);

			MemberInfo mem;
			if (sc.Reachable(FunctionName, out mem))
			{
				if ((func = mem as FunctionInfo) == null)
				{
					report.Add(new StaticError(line, column, $"Attempt to invoke a non-function member: {FunctionName}",
					                           ErrorLevel.Error));
					return false;
				}

				if (Arguments != null)
				{
					if (Arguments.Count != func.Parameters.Count)
					{
						report.Add(new StaticError(line, column,
											   $"Function {FunctionName} requires {func.Parameters.Count} and {Arguments.Count} was passed",
											   ErrorLevel.Error));
						return false;
					}

					for (int i = 0; i < Arguments.Count; i++)
					{
						var arg = Arguments[i];
						var type = func.Parameters[i].Item2;

						if (!arg.CheckSemantics(sc, report, type)) return false;

						if (arg.Return == type ||
						    ((type.Members != null ||
						      type.ArrayOf != null ||
						      type == _string) &&
						     arg.Return == _null)) continue;

						report.Add(new StaticError(arg.line, arg.column,
						                           $"Positional argument ({i}) must be of type {type}, but an expresion of type {arg.Return} was given",
						                           ErrorLevel.Error));
						return false;
					}
				}
				else if(func.Parameters.Count != 0)
				{
					report.Add(new StaticError(line, column,
					                           $"Function {FunctionName} requires {func.Parameters.Count} and 0 was passed",
					                           ErrorLevel.Error));
					return false;
				}
			}
			else
			{
				expected = expected ?? _void;

				if (expected == _null)
				{
					report.Add(new StaticError(line, column, "Return type can not be inferred", ErrorLevel.Error));
					return false;
				}

				if (Arguments != null)
					for (int i = 0; i < Arguments.Count; i++)
					{
						var arg = Arguments[i];

						if (!arg.CheckSemantics(sc, report)) return false;

						if (arg.Return != _void && arg.Return != _null) continue;

						report.Add(new StaticError(arg.line, arg.column,
						                           $"The type of positional argument ({i}) can not be inferred",
						                           ErrorLevel.Error));
						return false;
					}

				var def = new MemberDefinition
				{
					line = line,
					column = column,
					Generator = null,
					Member = new FunctionInfo
					{
						Name = FunctionName,
						//TODO: ver como decidir aliminar los nombres autogenerados
						Parameters = Arguments == null
							             ? new List<Tuple<string, TypeInfo>>()
							             : new List<Tuple<string, TypeInfo>>(from arg in Arguments
							                                                 select new Tuple<string, TypeInfo>("inferred_arg", arg.Return)),
						Return = expected,
						Pure = false
					}
				};

				if (!sc.Reachable(FunctionName, out mem, def))
					return false;

				func = (FunctionInfo)mem;

				string paramss = func.Parameters.Count > 0
					                 ? func.Parameters.Select(t => t.Item2.ToString()).Aggregate((h, t) => h + " X " + t)
					                 : "()";
				if (func.Parameters.Count > 1) paramss = $"({paramss})";

				report.Add(new StaticError(line, column,
				                           $"Declaration of function [{func.Name}: {paramss} -> {func.Return}] will be inferred",
				                           ErrorLevel.Warning));
			}

			Pure = func.Pure;
			if (Arguments != null)
				foreach (var arg in Arguments)
					Pure &= arg.Pure;

			Return = func.Return;
			if (func.Return != _void) ReturnValue = new HolderInfo {Type = Return};

			//TODO: ReturnValue.ConstValue
			return true;
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			H[] param = new H[Arguments?.Count ?? 0];
			if(Arguments != null)
				for (int i = 0; i < Arguments.Count; i++)
				{
					var arg = Arguments[i];
					arg.GenerateCode(cg, report);
					param[i] = (H)arg.ReturnValue.BCMMember;
				}

			H result = ReturnValue == null? null : cg.BindVar((T)Return.BCMMember);
			cg.Call((F)func.BCMMember, param, result);

			if (ReturnValue != null) ReturnValue.BCMMember = result;
		}
	}
}