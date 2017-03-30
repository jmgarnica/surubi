#define ENFORCE_RETURN_TYPE_CHECK
using System.Collections.Generic;
using TigerCs.CompilationServices;
using TigerCs.CompilationServices.AutoCheck;
using TigerCs.Generation.AST.Declarations;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Expressions
{
	public class Let : Expression
	{
		[NotNull]
		public List<IDeclarationList<IDeclaration>> Declarations { get; set; }

		public IExpression Body { get; set; }

		List<TypeInfo> declaredhere;

		public override bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{
			if (!this.AutoCheck(sc, report, expected)) return false;

			declaredhere = new List<TypeInfo>();
			sc.EnterNestedScope();

			for (int i = 0; i < Declarations.Count; i++)
			{
				sc.EnterNestedScope();
				if (Declarations[i].CheckSemantics(sc, report)) continue;
				sc.LeaveScope(i+2);
				return false;
			}

			if (!Body.CheckSemantics(sc, report, expected))
			{
				sc.LeaveScope(Declarations.Count + 1);
				return false;
			}

			sc.LeaveScope(Declarations.Count + 1);

			foreach (var dex in Declarations)
			{
				var dexlist = dex as IDeclarationList<TypeDeclaration>;
				if (dexlist == null) continue;
				foreach (var tdex in dexlist)
				{
					if(tdex is AliasDeclaration) continue;
					declaredhere.Add(tdex.DeclaredType);
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
			if (declaredhere.Contains(Body.Return))//TODO: fix this
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
			foreach (var declaration in Declarations)
				declaration.GenerateCode(cg,report);
			Body?.GenerateCode(cg, report);
			cg.LeaveScope();
		}
	}
}