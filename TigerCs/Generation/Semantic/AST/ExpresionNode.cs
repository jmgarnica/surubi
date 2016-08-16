using TigerCs.Generation.ByteCode;
using TigerCs.Generation.Semantic.Scopes;
using System;

namespace TigerCs.Generation.Semantic.AST
{
	public abstract class ExpresionNode
	{
		public bool CorrectSemantics { get; private set; }
		public TypeInfo Return { get; protected set; }

		public HolderInfo ReturnValue { get; protected set; }

		public void GenerateCode(ITigerEmitter te, ErrorReport report)
		{
			if (!CorrectSemantics) throw new InvalidOperationException("Can not generate while the node is not semantically correct");
			generate(te, report);
		}

		public abstract bool CheckSemantics(ISemanticStandar sp, ErrorReport report);

		public abstract void generate(ITigerEmitter cg, ErrorReport report);
	}
}
