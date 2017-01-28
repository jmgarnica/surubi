using System;
using System.Linq;
using Comand;

namespace Surubi
{
	class CompilerCMD
	{
		static int Main(string[] arg)
		{
			var c = new ComandParser<TigerGeneratorDescriptor>();

			TigerGeneratorDescriptor tg = null;
			if (arg.Length > 1)
				c.Activate(arg.Skip(1).Aggregate((s, t) => s + " " + t), out tg);
			if(tg == null)c.PrintHelp(Console.Out, "tg");

			return 0;
		}
	}
}
