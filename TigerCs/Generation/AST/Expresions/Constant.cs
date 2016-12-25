using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Expresions
{
	public class IntegerConstant : Expresion
	{
		int value;

		public override bool CheckSemantics(ISemanticChecker sp, ErrorReport report)
		{
			Return = sp.Int(report);
			ReturnValue = new HolderInfo { Type = Return };

			if (int.TryParse(Lex, out value)) return true;
			report.Add(new StaticError(line, column, "Integer parsing error", ErrorLevel.Error, Lex));
			return false;
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			ReturnValue.BCMMember = cg.AddConstant(value);
		}
	}

	public class StringConstant : Expresion
	{
		public override bool CheckSemantics(ISemanticChecker sp, ErrorReport report)
		{
			if (Lex == null)
			{
				report.Add(new StaticError(line, column, "String constant parsing error, null lex", ErrorLevel.Error));
				return false;
			}
			Return = sp.String(report);
			ReturnValue = new HolderInfo {Type = Return};
			return true;
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			ReturnValue.BCMMember = cg.AddConstant(Lex);
		}
	}

	public class NillConstant : Expresion
	{
		public override bool CheckSemantics(ISemanticChecker sp, ErrorReport report)
		{
			//Return = sp.Nill.Type;
			return true;
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			//ReturnValue = te.Nill;
		}
	}
}