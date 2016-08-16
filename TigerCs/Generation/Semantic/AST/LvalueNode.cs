using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TigerCs.Generation.ByteCode;
using TigerCs.Generation.Semantic.Scopes;

namespace TigerCs.Generation.Semantic.AST
{
	public abstract class LvalueNode : ExpresionNode, ILexToken
	{
		public int column
		{
			get;

			set;
		}

		public string Lex
		{
			get;

			set;
		}

		public int line
		{
			get;

			set;
		}
	}

	public class VarNode : LvalueNode
	{
		public override bool CheckSemantics(ISemanticStandar sp, ErrorReport report)
		{
			throw new NotImplementedException();
		}

		public override void generate(ITigerEmitter te, ErrorReport report)
		{
			throw new NotImplementedException();
		}
	}

	public class ArrayAccessNode : LvalueNode
	{
		ExpresionNode expresion;

		public override bool CheckSemantics(ISemanticStandar sp, ErrorReport report)
		{
			if (expresion.CheckSemantics(sp, report))
			{
				if (!expresion.Return.Type.Array)
				{
					report.Add(new TigerStaticError(line, column, "array access to non-array type", ErrorLevel.Error, Lex));
					return false;
				}
				//TODO: array underlaying type
			}
			return false;
		}

		public override void generate(ITigerEmitter te, ErrorReport report)
		{
			throw new NotImplementedException();
		}
	}
}