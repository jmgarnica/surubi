using TigerCs.Generation.ByteCode;
using TigerCs.Generation.Semantic.Scopes;

namespace TigerCs.Generation.Semantic.AST
{
	public abstract class ExpresionNode
	{
		public abstract bool CheckSemantics(TigerScope scope, ErrorReport report);

		public abstract void GenerateCode(ITigerEmitter cg);

		public abstract TypeInfo Return { get; }
	}
}
