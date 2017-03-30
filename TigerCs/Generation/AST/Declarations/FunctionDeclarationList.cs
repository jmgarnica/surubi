using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Declarations
{
	public class FunctionDeclarationList : DeclarationList<FunctionDeclaration>
	{
		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			foreach (var f in this)
				f.DeclareFunction(cg,report);

			foreach (var f in this)
				f.GenerateCode(cg, report);
		}
	}
}