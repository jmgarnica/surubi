namespace Command.Args
{
	public class CopToken : Token
	{
		public CopToken(string op) : base(op)
		{}

		public string CmpLex;

		public Lexer Composite;
	}
}