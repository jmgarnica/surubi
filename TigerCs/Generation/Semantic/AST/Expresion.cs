using TigerCs.Generation.ByteCode;
using System;

namespace TigerCs.Generation.Semantic.AST
{
	public abstract class Expresion
	{
		public int column { get; set; }

		public string Lex { get; set; }

		public int line { get; set; }

		public bool CorrectSemantics { get; private set; }
		public TypeInfo Return { get; protected set; }

		public HolderInfo ReturnValue { get; protected set; }

		public void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> te, ErrorReport report)
			where T : class, IType<T, F>
			where F : class, IFunction<T, F>
			where H : class, IHolder
		{
			if (!CorrectSemantics) throw new InvalidOperationException("Can not generate while the node is not semantically correct");
			Generate(te, report);
		}

		public abstract bool CheckSemantics(ISemanticChecker sc, ErrorReport report);

		protected abstract void Generate<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
			where T : class, IType<T, F>
			where F : class, IFunction<T, F>
			where H : class, IHolder;
	}
}
