using TigerCs.CompilationServices;
using TigerCs.CompilationServices.AutoCheck;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Expressions
{
	public class Break : Expression
	{
		LoopScopeDescriptor end;
		public override bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{
			Return = sc.Void();
			ReturnValue = null;
			Pure = false; /*TODO: Convertir pure en booleano de
			tres estados, null <=> el padre decide si es pure o no dependiendo del tipo de nodo*/
			CanBreak = true;

			end = sc.SeekDescriptor<LoopScopeDescriptor>(s => s is FunctionScopeDescriptor);
			if (end != null) return true;
			report.Add(new StaticError(line,column, "No enclosing loop out of which to break", ErrorLevel.Error));
			return false;
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			cg.Comment("Break");
			cg.Goto(end.ENDLabel);
		}
	}
}