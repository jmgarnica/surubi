using System;
using System.Linq;
using Command.Args;

namespace Surubi
{
	class CompilerCMD
	{
		static int Main(string[] arg)
		{
			var c = new ArgParse<TigerGeneratorDescriptor>();

			TigerGeneratorDescriptor tg = null;
			if (arg.Length > 1)
				tg = c.Activate(arg.Skip(1).Aggregate((s, t) => s + " " + t));
			if(tg == null)c.Help(Console.Out, "tg");

			return 0;
		}
	}
}
