using System;
using System.IO;

namespace Command.Args
{
	public interface IArgParse<out P>
		where P : class, new()
	{
		P Activate(string line);

		P Activate(Lexer lex);

		void RegisterComposite<T>()
			where T : class;

		void RegisterComposite(Type t);

		void RegisterParser(Type t, Func<string, ITuple<object, bool>> parser);

		void RegisterParser<T>(Func<string, ITuple<T, bool>> parser)
			where T : class;

		void Help(TextWriter t, string command = null);
	}
}
