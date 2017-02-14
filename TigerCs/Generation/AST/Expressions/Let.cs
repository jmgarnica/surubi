#define ENFORCE_RETURN_TYPE_CHECK
using System.Collections.Generic;
using TigerCs.CompilationServices;
using TigerCs.Generation.AST.Declarations;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Expressions
{
	public class Let : Expression
	{
		[Release(true)]
		public IDeclarationList<IDeclarationList<IDeclaration>> Declarations { get; set; }

		[Release]
		public IExpression Body { get; set; }

		List<TypeInfo> declaredhere;

		public override bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{
			declaredhere = new List<TypeInfo>();
			sc.EnterNestedScope();

			if (Declarations != null)
			{
				foreach (var dex in Declarations)
					dex.BindName(sc,report);

				foreach (var dex in Declarations)
					if (!dex.CheckSemantics(sc, report)) return false;

				foreach (var dex in Declarations)
				{
					var dexlist = dex as IDeclarationList<TypeDeclaration>;
					if (dexlist == null) continue;
					foreach (var tdex in dexlist)
					{
						if(tdex is AliasDeclaration) continue;
						declaredhere.Add(tdex.Type);
					}
				}
			}

			var _void = sc.Void(report);

			if (Body == null)
			{
				Return = _void;
				ReturnValue = null;
				Pure = true;
				CanBreak = false;
				return true;
			}

#if ENFORCE_RETURN_TYPE_CHECK
			if (declaredhere.Contains(Body.Return))
			{
				report.Add(new StaticError(line, column, $"The return type {Body.Return} is not visible in the outer scope",
				                           ErrorLevel.Error));
				return false;
			}
#endif

			Return = Body.Return;
			ReturnValue = Return.Equals(_void)? null : new HolderInfo {ConstValue = Body.ReturnValue?.ConstValue, Type = Return};
			Pure = Body.Pure;
			CanBreak = Body.CanBreak;
			return true;
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			cg.EnterNestedScope(declaredhere.Count > 0, "LET");
			Declarations?.GenerateCode(cg,report);
			Body?.GenerateCode(cg, report);
			cg.LeaveScope();
		}
	}
}