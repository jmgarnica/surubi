using System;
using System.IO;
using Command.Args;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Surubi;
using TigerCs.Emitters.NASM;
using TigerCs.Generation.ByteCode;

namespace CMPTest.Emission
{
	[TestClass]
	public class NasmEmissionTester : EmissionTester<NasmType, NasmFunction, NasmHolder>
	{
		NasmDescriptor nd;

		protected override IByteCodeMachine<NasmType, NasmFunction, NasmHolder> InitBCM(string testname)
		{
			var d = new ArgParse<NasmDescriptor>();
			nd = d.Activate("");
			nd.OutputFile = testname;
			return (NasmEmitter)nd.GetBCM();
		}

		protected override string Run(string[] args, string testdata, out int exitcode)
		{
			return nd.Run(args, testdata, r, false, out exitcode);
		}

		protected override void Clear(string testname)
		{
			try
			{
				File.Delete(testname + ".asm");
				File.Delete(testname + ".o");
				DirectoryInfo di = new DirectoryInfo("../../../TestResults/builds/");
				if (!di.Exists) di.Create();
				var exe = testname + ".exe";
				File.Delete(di.FullName + exe);
				File.Move(exe, di.FullName + exe);
			}
			catch (Exception)
			{
				// ignored
			}
		}
	}
}
