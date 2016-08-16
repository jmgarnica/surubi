using System;
using TigerCs.Generation.ByteCode;
using TigerCs.Generation.Semantic.Scopes;

namespace TigerCs.Generation.Semantic.AST
{
	public abstract class ConstantNode : ExpresionNode, ILexToken
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

	public class IntegerConstantNode : ConstantNode
	{
		int value;

		public override bool CheckSemantics(ISemanticStandar sp, ErrorReport report)
		{
			if (!int.TryParse(Lex, out value))
			{
				report.Add(new TigerStaticError(line, column, "parsing error", ErrorLevel.Error, Lex));
				return false;
			}
			Return = sp.Int;
			return true;
		}

		public override void generate(ITigerEmitter te, ErrorReport report)
		{
			ReturnValue = new HolderInfo
			{
				Bounded = true,
				Holder = te.AddConstant(value),
				Name = "",
				Type = Return
			};
		}
	}

	public class StringConstantNode : ConstantNode
	{
		public override bool CheckSemantics(ISemanticStandar sp, ErrorReport report)
		{
			if (Lex == null)
			{
				report.Add(new TigerStaticError(line, column, "parsing error, null lex", ErrorLevel.Error));
				return false;
			}
			Return = sp.String;
			return true;
		}

		public override void generate(ITigerEmitter te, ErrorReport report)
		{
			ReturnValue = new HolderInfo
			{
				Bounded = true,
				Holder = te.AddConstant(Lex),
				Name = "",
				Type = Return
			};
		}
	}

	public class NillConstantNode : ConstantNode
	{
		public override bool CheckSemantics(ISemanticStandar sp, ErrorReport report)
		{
			Return = sp.Nill.Type;
			return true;
		}

		public override void generate(ITigerEmitter te, ErrorReport report)
		{
			ReturnValue = te.Nill;
		}
	}
}