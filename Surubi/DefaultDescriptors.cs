using System;
using System.IO;
using System.Linq;
using Command.Args;
using TigerCs.CompilationServices;
using TigerCs.Emitters;
using TigerCs.Emitters.NASM;
using TigerCs.Generation;
using TigerCs.Generation.ByteCode;

namespace Surubi
{

	[ArgumentCandidate(Candidates = new []{typeof(SemanticCheckerDescriptor), typeof(NasmDescriptor)}, AfterInitializationActionMethod = nameof(initialize))]
	public class TigerGeneratorDescriptor
	{
		[Argument(OptionName = "chk", Help = "Semantic checking protocol", DefaultValue = "dft")]
		public SemanticCheckerDescriptor Checker { get; set; }

		[Argument(OptionName = "bcm", Help = "a target engine for code generation", DefaultValue = "nasm")]
		public BCMDescriptor BCM { get; set; }

		[Argument(positionalargument = 0, Help = "the path of the imput file")]
		public string Source { get; set; }

		public IGenerator Generator { get; private set; }

		void initialize()
		{
			var bcm = BCM.GetBCM();
			var t = from i in bcm.GetType().GetInterfaces()
					where i.GetGenericTypeDefinition() == typeof(IByteCodeMachine<,,>)
					select i.GetGenericArguments();

			var t_gen = typeof(TigerGenerator<,,>).MakeGenericType(t.First());

			var constructor_info = t_gen.GetConstructor(Type.EmptyTypes);
			if (constructor_info == null) throw new InvalidOperationException();

			object gen = constructor_info.Invoke(new object[0]);
			t_gen.GetProperty("ByteCodeMachine").SetValue(gen, bcm);
			t_gen.GetProperty("SemanticChecker").SetValue(gen, Checker.InitializeChecker());

			Generator = (IGenerator)gen;
		}

	}

	[ArgumentCandidate(Help = "Default Semantic Checker for Tiger programs", OptionName = "dft")]
	public class SemanticCheckerDescriptor
	{
		public virtual ISemanticChecker InitializeChecker()
		{
			return new DefaultSemanticChecker();
		}
	}

	public abstract class BCMDescriptor
	{
		public abstract object GetBCM();
	}

	[ArgumentCandidate(OptionName = "nasm", Help = "generate nasm assambler code")]
	public class NasmDescriptor : BCMDescriptor
	{
		[Argument(DefaultValue = "tg", OptionName = "o", Help = "[out]: the name of the output")]
		public string OutputFile { get; set; }

		[Argument(flag = true, OptionName = "b", DefaultValue = true,
			Help = "if set the output will be a binary executable, if not will be nasm source code")]
		public bool Binary { get; set; }

		[Argument(OptionName = "ap", DefaultValue = @".\NASM\NASM\nasm.exe",
			Help = "the path to the directory containing nasm assember," +
			       " this defines [asm_dir_paath] as the directory containing the assembler")]
		public string AssemblerPath { get; set; }

		[Argument(OptionName = "lp", DefaultValue = @".\NASM\MinGW\bin\gcc.exe",
			Help = "the path to the linker")]
		public string LinkerPath { get; set; }

		[Argument(OptionName = "ao", DefaultValue = @"-g -f win32 {out}.asm -o {out}.o",
			Help = "command line arguments for the assembler")]
		public string AssemblerOptions { get; set; }

		[Argument(OptionName = "lo", DefaultValue = @"{out}.o {asm_dir_paath}\macro.o -g -o {out}.exe -m32",
			Help = "command line arguments for the linker")]
		public string LinkerOptions { get; set; }

		public override object GetBCM()
		{
			var assdir = new FileInfo(AssemblerPath).Directory?.FullName;
			AssemblerOptions = AssemblerOptions.Replace("{out}", OutputFile)
			                                   .Replace("{asm_dir_paath}", assdir
			                                                               ??
			                                                               Environment.CurrentDirectory);
			LinkerOptions = LinkerOptions.Replace("{out}", OutputFile)
			                             .Replace("{asm_dir_paath}", assdir
			                                                         ??
			                                                         Environment.CurrentDirectory);

			AssemblerPath = AssemblerPath.Replace(" ", @"\ ");
			LinkerPath = LinkerPath.Replace(" ", @"\ ");

			return new NasmEmitter(OutputFile)
			{
				OnEndBuild = new NasmBuild
				{
					AssemblerPath = AssemblerPath,
					AssemblerOptions = AssemblerOptions,
					LinkerOptions = LinkerOptions,
					LinkerPath = LinkerPath
				}
			};
		}
	}
}
