using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
			//Console.WriteLine(arg.Length);

			var c = new ArgParse<TigerGeneratorDescriptor>();

			TigerGeneratorDescriptor tg = null;
			if (arg.Length > 0)
				tg = c.Activate(arg.Aggregate((s, t) => s + " " + t));
			if (tg == null)
				PrintHelp();
			else
			{
				if (!File.Exists(tg.Source))
				{
					Console.WriteLine(new StaticError(0, 0, $"{tg.Source}: no such file or directory", ErrorLevel.Error).PlainFormat());
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

				tg.Generator.Compile(code, report, FillTigerStd(report));

				if (report.Count() != 0)
				{
					report.prntReport();
					return 1;
				}

				Console.WriteLine("Compilation Success");
			}
			return 0;
		}

		static void PrintHelp()
		{
			Console.WriteLine("usage: tiger.exe <source> -p [parser options] -chk [checker options] -bcm [bcm options]");
			Console.WriteLine("parser options: tiger");
			Console.WriteLine("checker options: dft");
			Console.WriteLine("emiter options: nasm");
			Console.WriteLine("nasm options:");
			Console.WriteLine("-o output_file (default 'tg')");
			Console.WriteLine("-b (binary output flag, default: true)");
			Console.WriteLine("-ap assembler_path (default '.\\NASM\\NASM\\nasm.exe')");
			Console.WriteLine("-lp linker_path (default '.\\NASM\\MinGW\\bin\\gcc.exe')");
			Console.WriteLine("-ao assembler_options (default '-g -f win32 {out}.asm -o {out}.o')");
			Console.WriteLine("-lo linker_options (default '{asm_dir_path}\\clink.o {asm_dir_path}\\std.o {out}.o -g -o {out}.exe -m32')");
			Console.WriteLine();
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
			TypeInfo _string, _int, _void;
			var gen = new Dictionary<string, MemberDefinition>
			{
				{TypeInfo.MakeTypeName(MemberInfo.MakeCompilerName("void")), new MemberDefinition(_void = new TypeInfo {Name = "void", BCMBackup = false}, r)},
				{TypeInfo.MakeTypeName("string"), new MemberDefinition(_string = new TypeInfo {Name = "string"}, r)},
				{TypeInfo.MakeTypeName("int"), new MemberDefinition(_int = new TypeInfo {Name = "int"}, r)},
				{"getline", new MemberDefinition(new FunctionInfo {Name = "getline", Parameters = new List<System.Tuple<string, TypeInfo>> (), Return = _string }, r)},
				{"size", new MemberDefinition(new FunctionInfo {Name = "size", Parameters = new List<System.Tuple<string, TypeInfo>> {Tuple.Create("s", _string)}, Return = _int }, r)},
				{"print", new MemberDefinition(new FunctionInfo {Name = "print", Parameters = new List<System.Tuple<string, TypeInfo>> {Tuple.Create("s", _string)}, Return = _void }, r)},
				{"printi", new MemberDefinition(new FunctionInfo {Name = "printi", Parameters = new List<System.Tuple<string, TypeInfo>> {Tuple.Create("i", _int)}, Return = _void }, r)},
				{"ord", new MemberDefinition(new FunctionInfo {Name = "ord", Parameters = new List<System.Tuple<string, TypeInfo>> {Tuple.Create("s", _string)}, Return = _int }, r)},
				{"chr", new MemberDefinition(new FunctionInfo {Name = "chr", Parameters = new List<System.Tuple<string, TypeInfo>> {Tuple.Create("i", _int)}, Return = _string }, r)},
				{"substring", new MemberDefinition(new FunctionInfo {Name = "substring", Parameters = new List<System.Tuple<string, TypeInfo>> {Tuple.Create("s", _string), Tuple.Create("f",_int), Tuple.Create("n", _int)}, Return = _string }, r)},
				{"concat", new MemberDefinition(new FunctionInfo {Name = "concat", Parameters = new List<System.Tuple<string, TypeInfo>> {Tuple.Create("a", _string), Tuple.Create("b",_string)}, Return = _string }, r)},
				{"not", new MemberDefinition(new FunctionInfo {Name = "not", Parameters = new List<System.Tuple<string, TypeInfo>> {Tuple.Create("i", _int)}, Return = _int }, r)},
				{"exit", new MemberDefinition(new FunctionInfo {Name = "exit", Parameters = new List<System.Tuple<string, TypeInfo>> {Tuple.Create("i", _int)}, Return = _void }, r)},
				{"printline", new MemberDefinition(new FunctionInfo {Name = "printline", Parameters = new List<System.Tuple<string, TypeInfo>> {Tuple.Create("s", _string)}, Return = _void }, r)},
				{"printiline", new MemberDefinition(new FunctionInfo {Name = "printiline", Parameters = new List<System.Tuple<string, TypeInfo>> {Tuple.Create("i", _int)}, Return = _void }, r)},
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
