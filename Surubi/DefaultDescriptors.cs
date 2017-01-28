using System;
using System.Linq;
using Comand;
using TigerCs.CompilationServices;
using TigerCs.Emitters;
using TigerCs.Emitters.NASM;
using TigerCs.Generation;

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
					where i.IsGenericType && i.Name == "IByteCodeMachine`3"
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
	//TODO: simple args should: -chk dft
	public abstract class BCMDescriptor
	{
		public abstract object GetBCM();
	}

	[ArgumentCandidate(OptionName = "nasm", Help = "generate nasm assambler code")]
	public class NasmDescriptor : BCMDescriptor
	{
		[Argument(DefaultValue = "tg.asm", OptionName = "o", Help = "the name of the output")]
		public string OutputFile { get; set; }

		public override object GetBCM()
		{
			return new NasmEmitter(OutputFile);
		}
	}
}
