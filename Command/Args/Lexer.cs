using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Command.Args
{
	public class Lexer : IEnumerable<Token>
	{
		readonly string line;

		public Lexer(string line)
		{
			this.line = line.Trim() + " ";
		}

		/// <summary>Returns an enumerator that iterates through the collection.</summary>
		/// <returns>A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.</returns>
		public IEnumerator<Token> GetEnumerator()
		{
			int curr = 0;
			int last = 0;
			int brackets = 0;
			string comp = "";
			bool op = line[0] == '-';
			bool compsearch = false;

			while (curr < line.Length)
			{
				switch (line[curr])
				{
					case ' ':
						if (brackets == 0)
						{
							string lex;
							if (!string.IsNullOrEmpty(comp))
							{
								lex = line.Substring(last, curr - 2 - last + 1).Trim();
								yield return
									new CopToken(comp)
									{
										Char = last,
										Composite = new Lexer(lex),
										Op = false,
										CmpLex = comp + ": " + lex
									};
							}
							else
							{
								lex = line.Substring(last + (op? 1 : 0), curr - last + (op? 0 : 1)).Trim();
								if (!string.IsNullOrWhiteSpace(lex))
									yield return
										new Token(lex)
										{
											Char = last,
											Op = op
										};
							}
							consume(' ', line, ref curr);
							if (curr < line.Length) op = line[curr] == '-';
							comp = "";
							compsearch = false;
							last = curr;
							curr--;
						}
						break;

					case '[':
						brackets++;
						compsearch = brackets == 1;
						break;

					case ']':
						brackets--;
						if(brackets < 0)yield break;
						compsearch = false;
						curr++;
						goto case ' ';

					case ':':
						if (compsearch)
						{
							comp = line.Substring(last +1 , curr - last -1).Trim();
							last = curr + 1;
							compsearch = false;
						}
						break;
				}
				curr++;
			}
		}

		static void consume(char t, string line, ref int pos)
		{
			while (pos < line.Length && line[pos] == t)
				pos++;
		}

		/// <summary>Returns an enumerator that iterates through a collection.</summary>
		/// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <summary>Returns a string that represents the current object.</summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			StringBuilder b = new StringBuilder(line.Length);
			CopToken c;
			foreach (var t in this)
			{
				if ((c = t as CopToken) != null)
				{
					b.Append('[');
					b.Append(c.Lex);
					b.Append(':');
					b.Append(' ');
					b.Append(c.Composite);
					b.Append(']');
				}
				else b.Append(t.Lex);

				b.Append(' ');
			}

			return b.ToString();
		}
	}
}
