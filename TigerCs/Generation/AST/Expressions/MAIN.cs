﻿using System;
using System.Collections.Generic;
using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Expressions
{
	public sealed class MAIN : Expression
	{
		public const string EntryPointName = "Main";
		public const string ArgumentName = "args";
		public const string PrintSName = "prints";

		TypeInfo _string, _int, _void, _null, arrayof_string;
		FunctionInfo prints, Main;
		HolderInfo args;

		[NotNull]
		[Release]
		public IExpression Root { get; }

		public MAIN(IExpression root)
		{
			Root = root;
			Main = null;
			arrayof_string = null;
		}


		public override bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{
			sc.EnterNestedScope();
			_string = sc.String(report);
			_int = sc.Int(report);
			_void = sc.Void(report);
			_null = sc.Null(report);

			arrayof_string = new TypeInfo
			{
				Name = TypeInfo.MakeArrayName(_string.Name),
				ArrayOf = _string
			};

			sc.DeclareMember(TypeInfo.MakeTypeName(arrayof_string.Name), new MemberDefinition
				                 {Member = arrayof_string, column = column, line = line});

			Main = new FunctionInfo
			{
				Name = EntryPointName,
				Parameters = new List<Tuple<string, TypeInfo>>
				{
					new Tuple<string, TypeInfo>(ArgumentName, arrayof_string)
				},
				Return = _int
			};

			sc.DeclareMember(Main.Name, new MemberDefinition {Member = Main, column = column, line = line});

			args = new HolderInfo
			{
				Name = ArgumentName,
				Type = arrayof_string,
			};

			sc.DeclareMember(args.Name, new MemberDefinition {Member = args, line = line, column = column});

			Return = _void;

			if(!Root.CheckSemantics(sc,report)) return false;
			sc.LeaveScope();

			if (Root.Return == _string)
			{
				MemberInfo ps;
				if (!sc.Reachable(PrintSName, out ps, new MemberDefinition
				                  {
					                  Generator = null,
					                  Member = new FunctionInfo
					                  {
						                  Return = _void,
						                  Name = PrintSName,
						                  Parameters = new List<Tuple<string, TypeInfo>> {new Tuple<string, TypeInfo>("s", _string)}
					                  },
									  line = line,
									  column = column
				                  }))
				{
					report.Add(new StaticError(line, column,
					                           $"There is no definition for ({PrintSName}), and the return value of the program is {_string}",
					                           ErrorLevel.Internal));
					return false;
				}
				prints = ps as FunctionInfo;
				return true;
			}
			if (Root.Return == _int || Root.Return == _void || Root.Return == _null) return true;

			report.Add(new StaticError(0, 0, "A program must return a value of type integer or string, or don't return any",
			                           ErrorLevel.Error));
			return false;
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			if (!arrayof_string.Bounded) arrayof_string.BCMMember = cg.BindArrayType(arrayof_string.Name, (T)_string.BCMMember);

			Main.BCMMember = cg.EntryPoint(true, true);
			args.BCMMember = cg.GetParam(0);

			Root.GenerateCode(cg, report);

			if (Root.Return.Equals(_int))
				cg.Ret((H)Root.ReturnValue.BCMMember);
			else if (Root.Return.Equals(_string))
			{
				cg.Call((F)prints.BCMMember, new[] {(H)Root.ReturnValue.BCMMember});
				cg.Ret(cg.AddConstant(0));
			}
			else
				cg.Ret(cg.AddConstant(0));

			cg.LeaveScope();
		}
	}
}