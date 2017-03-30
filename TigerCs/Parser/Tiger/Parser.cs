using System;
using System.IO;
using Antlr.Runtime;
using TigerCs.CompilationServices;
using TigerCs.Generation.AST.Expressions;

namespace TigerCs.Parser.Tiger
{
	class Parser : IParser
	{
		public IExpression Parse(TextReader tr, ErrorReport tofill)
		{
			try
			{
				TigrammarLexer lexer = new TigrammarLexer(new ANTLRReaderStream(tr));
				TigrammarParser parser = new TigrammarParser(new CommonTokenStream(lexer));
				var exp = parser.program();
				return exp;
			}
			catch (Exception e)
			{
				tofill.Add(new StaticError(0, 0, $"Parser Error {(string.IsNullOrWhiteSpace(e.Message)? "" : ": " + e.Message)}",
				                           ErrorLevel.Internal));
				return null;
			}
        }
    }
}
