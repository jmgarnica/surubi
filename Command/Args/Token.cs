namespace Command.Args
{
	public class Token
	{
		public Token(string lex)
		{
			Lex = lex.Trim(' ');
			if (Lex[0] == '[') Lex = Lex.Substring(1);
			if (Lex[Lex.Length - 1] == ']') Lex = Lex.Substring(0, Lex.Length - 1);
			Lex = Lex.Trim();
		}

		public int Char;
		public string Lex { get; }
		public bool Op;
	}
}