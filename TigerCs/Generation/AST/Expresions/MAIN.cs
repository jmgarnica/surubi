using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Expresions
{
	public sealed class MAIN : Expresion
	{
		public static FunctionInfo Main { get; private set; }
		public static TypeInfo ArrayOfString { get; private set; }
		public const string EntryPointName = "Main";
		public const string ArgumentName = "args";

		public IExpresion root { get; private set; }
		public HolderInfo args { get; private set; }

		public MAIN(IExpresion root)
		{
			this.root = root;
			Main = null;
			ArrayOfString = null;
		}

		TypeInfo _string;

		public override bool CheckSemantics(ISemanticChecker sc, ErrorReport report)
		{
			sc.EnterNestedScope();
			_string = sc.String(report);
			ArrayOfString = new TypeInfo
			{
				Name = TypeInfo.MakeArrayName(_string.Name),
				ArrayOf = _string,
			};

			sc.DeclareMember(ArrayOfString.Name, ArrayOfString);

			Main = new FunctionInfo
			{
				Name = EntryPointName,
				BackupDefintion = true,
				Parameters = new List<Tuple<string, TypeInfo>> { new Tuple<string, TypeInfo>(ArgumentName, ArrayOfString) },
				Return = sc.Int(report)
			};

			sc.DeclareMember(Main.Name, Main);

			args = new HolderInfo
			{
				Name = ArgumentName,
				Type = ArrayOfString,
			};

			sc.DeclareMember(args.Name, args);

			Return = sc.Void(report);

			if(!root.CheckSemantics(sc,report)) return false;
			sc.LeaveScope();
			return true;
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			if (!ArrayOfString.Bounded) ArrayOfString.BCMMember = cg.BoundArrayType(ArrayOfString.Name, (T)_string.BCMMember);

			Main.BCMMember = cg.EntryPoint(true, true);
			args.BCMMember = cg.GetParam(0);

			root.GenerateCode(cg, report);
			if (!root.Return.Equals(Return))
			{
				cg.Ret((H)root.ReturnValue.BCMMember);
			}
			cg.LeaveScope();
		}

		public override void Dispose()
		{
			root.Dispose();
			Main = null;
			ArrayOfString = null;
		}
	}
}
