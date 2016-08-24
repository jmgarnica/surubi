using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.Semantic.AST
{
	public abstract class Constant : Expresion
	{ }

	public class IntegerConstant : Constant
	{
		int value;

		public override bool CheckSemantics(ISemanticChecker sp, ErrorReport report)
		{
			//if (!int.TryParse(Lex, out value))
			//{
			//	report.Add(new TigerStaticError(line, column, "parsing error", ErrorLevel.Error, Lex));
			//	return false;
			//}
			////Return = sp.Int;
			return true;
		}

		protected override void Generate<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			ReturnValue = new HolderInfo
			{
				//Bounded = true,
				Holder = cg.AddConstant(value),
				Name = "",
				Type = Return
			};
		}
	}

	public class StringConstant : Constant
	{
		public override bool CheckSemantics(ISemanticChecker sp, ErrorReport report)
		{
			//if (Lex == null)
			//{
			//	report.Add(new TigerStaticError(line, column, "parsing error, null lex", ErrorLevel.Error));
			//	return false;
			//}
			//Return = sp.String;
			return true;
		}

		protected override void Generate<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			ReturnValue = new HolderInfo
			{
				//Bounded = true,
				//Holder = te.AddConstant(Lex),
				//Name = "",
				//Type = Return
			};
		}
	}

	public class NillConstant : Constant
	{
		public override bool CheckSemantics(ISemanticChecker sp, ErrorReport report)
		{
			//Return = sp.Nill.Type;
			return true;
		}

		protected override void Generate<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			//ReturnValue = te.Nill;
		}
	}
}