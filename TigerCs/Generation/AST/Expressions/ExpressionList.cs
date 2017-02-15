using System.Collections.Generic;
using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Expressions
{
	public class ExpressionList<E> : List<E>, IExpression
		where E : IExpression
	{
		public bool CanBreak { get; protected set; }

		public int column { get; set; }
		public string Lex { get; set; }
		public int line { get; set; }

		public bool Pure { get; protected set; }

		public TypeInfo Return { get; protected set; }
		public HolderInfo ReturnValue { get; protected set; }

		TypeInfo _void;
		public bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{
			CanBreak = false;
			Pure = true;

			_void = sc.Void(report);

			foreach (var item in this)
			{
				if (!item.CheckSemantics(sc, report)) return false;

				Pure &= item.Pure;
				CanBreak |= item.CanBreak;
			}

			if (Count > 0)
			{
				Return = CanBreak? _void : this[Count - 1].Return;
				ReturnValue = CanBreak? null : this[Count - 1].ReturnValue;
			}
			else
			{
				Return = _void;
				ReturnValue = null;
			}

			return true;
		}

		public void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
			where T : class, IType<T, F>
			where F : class, IFunction<T, F>
			where H : class, IHolder
		{
			for (int i = 0; i < Count - 1; i++)
			{
				var item = this[i];
				item.GenerateCode(cg, report);
				if(item.ReturnValue != null)
					cg.Release((H)item.ReturnValue.BCMMember);
			}

			this[Count - 1].GenerateCode(cg, report);
		}
	}
}
