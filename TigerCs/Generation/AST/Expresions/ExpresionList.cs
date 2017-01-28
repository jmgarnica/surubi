using System.Collections.Generic;
using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Expresions
{
	public class ExpresionList<E> : List<E>, IExpresion
		where E : IExpresion
	{
		public int column { get; set; }
		public bool CorrectSemantics { get; protected set; }
		public string Lex { get; set; }
		public int line { get; set; }

		public bool Pure { get; protected set; }

		public TypeInfo Return { get; protected set; }
		public HolderInfo ReturnValue { get; protected set; }

		public bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{
			foreach (var item in this)
				if (!item.CheckSemantics(sc, report)) return false;

			if (Count > 0)
			{
				Return = this[Count - 1].Return;
				ReturnValue = this[Count - 1].ReturnValue;
			}

			return true;
		}

		public void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
			where T : class, IType<T, F>
			where F : class, IFunction<T, F>
			where H : class, IHolder
		{
			foreach (var item in this)
				item.GenerateCode(cg, report);
		}
	}
}
