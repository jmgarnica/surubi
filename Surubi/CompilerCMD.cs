using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Command.Args;
using TigerCs.CompilationServices;
using TigerCs.Generation;
using TigerCs.Generation.AST.Expressions;

namespace Surubi
{
	public static class CompilerCMD
	{
		static int Main(string[] arg)
		{
			prntIntro();

			var c = new ArgParse<TigerGeneratorDescriptor>();

			TigerGeneratorDescriptor tg = null;
			if (arg.Length > 1)
				tg = c.Activate(arg.Skip(1).Aggregate((s, t) => s + " " + t));
			if (tg == null) c.Help(Console.Out, "tg");
			else
			{
				if (!File.Exists(tg.Source))
				{
					Console.WriteLine(new StaticError(0,0, $"{tg.Source}: no such file or directory", ErrorLevel.Error).PlainFormat());
					return 1;
				}

				ErrorReport report = new ErrorReport();
				IExpression code;

				using (var f = new FileStream(tg.Source, FileMode.Open, FileAccess.Read))
				{
					code = tg.Generator.Parse(new StreamReader(f), report);
				}

				report.prntReport();

				if (code == null)
					return 1;

				report.Clear();

				tg.Generator.Compile(code, report);

				if (report.Count() != 0)
				{
					report.prntReport();
					return 1;
				}

				Console.WriteLine("Compilation Success");
			}
			return 0;
		}

		static void prntReport(this ErrorReport r)
		{
			var ncolor = Console.ForegroundColor;
			foreach (var error in r)
			{
				switch (error.Level)
				{
					case ErrorLevel.Info:
					case ErrorLevel.Warning:
						Console.ForegroundColor = ConsoleColor.Gray;
						break;
					case ErrorLevel.Error:
					case ErrorLevel.Internal:
						Console.ForegroundColor = ConsoleColor.Red;
						break;
					default:
						Console.ForegroundColor = ncolor;
						break;
				}
				Console.WriteLine(error);
			}
			Console.ForegroundColor = ncolor;
		}

		public static IDictionary<string, MemberDefinition> FillTigerStd(ErrorReport r)
		{
			var gen = new Dictionary<string, MemberDefinition>
			{
				{TypeInfo.MakeTypeName("string"), new MemberDefinition(new TypeInfo {Name = "string"}, r)},
				{TypeInfo.MakeTypeName("int"), new MemberDefinition(new TypeInfo {Name = "int"}, r)}
			};

			return gen;
		}

		static void prntIntro()
		{
			Console.WriteLine("Tiger Compiler version 1.0");
			Console.WriteLine("Copyright (C) Carlos David Muñis Chall && Jesús Manuel Garnica Bonome");
		}
	}
}
