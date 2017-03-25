using System;
using System.Diagnostics;
using System.IO;
using Command.Args;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Surubi;
using TigerCs.CompilationServices;
using TigerCs.Emitters.NASM;
using TigerCs.Generation.ByteCode;

namespace CMPTest.Emission
{
	[TestClass]
	public class NasmEmissionTester : EmissionTester<NasmType, NasmFunction, NasmHolder>
	{
		protected override IByteCodeMachine<NasmType, NasmFunction, NasmHolder> InitBCM(string testname)
		{
			var d = new ArgParse<NasmDescriptor>();
			NasmDescriptor nd = d.Activate("");
			nd.OutputFile = testname;
			return (NasmEmitter)nd.GetBCM();
		}

		public int Wait { get; set; } = int.MaxValue;

		protected override string Run(string[] args, string testdata, out int exitcode)
		{
			try
			{
				var a = (NasmEmitter)e;
				var ass = new Process
				{
					StartInfo =
					{
						Arguments = args != null && args.Length > 0? string.Join(" ", args) : "",
						FileName = a.OutputFile + ".exe",
						UseShellExecute = false,
						RedirectStandardError = true,
						RedirectStandardOutput = true,
						RedirectStandardInput = true
					}
				};
				ass.Start();
				Console.WriteLine($"NASM: Test start time: {ass.StartTime:G}");
				if (!ass.WaitForExit(Wait))
					try
					{
						ass.Kill();
						exitcode = -1000;
						return "";
					}
					finally
					{
						r.Add(new StaticError(0, 0,
						                      "Assembling is taking to long to complete, try with other assember or provide more time",
						                      ErrorLevel.Error));
					}

				if (ass.ExitCode != 0)
				{
					r.Add(new StaticError(0, 0, "Assembler fail",
					                      ErrorLevel.Error));
					Console.WriteLine(ass.StandardOutput.ReadToEnd());
					Console.WriteLine(ass.StandardError.ReadToEnd());
					exitcode = ass.ExitCode;
					return "";
				}

				Console.WriteLine($"NASM: Test exit time: {ass.ExitTime:G}");
				exitcode = ass.ExitCode;
				return ass.StandardOutput.ReadToEnd();
			}
			catch (Exception f)
			{
				r.Add(new StaticError(0, 0, $"Run aborted: {f.Message}", ErrorLevel.Error));
				Debugger.Break();
				exitcode = -1000;
				return "";
			}
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
