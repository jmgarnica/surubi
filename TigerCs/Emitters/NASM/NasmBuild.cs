using System;
using System.Diagnostics;
using TigerCs.CompilationServices;

namespace TigerCs.Emitters.NASM
{
	public class NasmBuild
	{
		public int WaitSeconds { get; set; } = 30;

		public string AssemblerPath { get; set; }

		public string LinkerPath { get; set; }

		public string AssemblerOptions { get; set; }

		public string LinkerOptions { get; set; }

		public virtual void Build(string outputFile, ErrorReport r)
		{
			var ass = new Process
			{
				StartInfo =
				{
					Arguments = AssemblerOptions,
					FileName = AssemblerPath,
					UseShellExecute = false,
					RedirectStandardError = true,
					RedirectStandardOutput = true
				}
			};


			ass.Start();
			Console.WriteLine($"NASM: Assembler start time: {ass.StartTime:G}");
			if (!ass.WaitForExit(WaitSeconds * 1000))
				try
				{
					ass.Kill();
					return;
				}
				finally
				{
					r.Add(new StaticError(0, 0, "Assembling is taking to long to complete, try with other assember or provide more time",
					                      ErrorLevel.Error));
				}

			if (ass.ExitCode != 0)
			{
				r.Add(new StaticError(0, 0, "Assembler fail",
				                      ErrorLevel.Error));
				Console.WriteLine(ass.StandardOutput.ReadToEnd());
				Console.WriteLine(ass.StandardError.ReadToEnd());
				return;
			}

			Console.WriteLine($"NASM: Assembler exit time: {ass.ExitTime:G}");

			ass = new Process
			{
				StartInfo =
				{
					Arguments = LinkerOptions,
					FileName = LinkerPath,
					UseShellExecute = false,
					RedirectStandardError = true,
					RedirectStandardOutput = true
				}
			};

			ass.Start();
			Console.WriteLine($"NASM: Linker start time: {ass.StartTime:G}");
			if (!ass.WaitForExit(WaitSeconds * 1000))
				try
				{
					ass.Kill();
					return;
				}
				finally
				{
					r.Add(new StaticError(0, 0, "Linking is taking to long to complete, try with other linker or provide more time",
										  ErrorLevel.Error));
				}

			if (ass.ExitCode == 0)
			{
				Console.WriteLine($"NASM: Linker exit time: {ass.ExitTime:G}");
				return;
			}
			r.Add(new StaticError(0, 0, "Linker fail",
			                      ErrorLevel.Error));
			Console.WriteLine(ass.StandardOutput.ReadToEnd());
			Console.WriteLine(ass.StandardError.ReadToEnd());
		}
	}
}
