using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TigerCs.CompilationServices
{
	public static class Extensions
	{
		public static string MinToString(this Guid g)
		{
			string s = "";
			var b = g.ToByteArray();
			byte nonzero = 0;
			for (int i = 0; i < b.Length; i++)
			{
				if (b[i] != 0)
					nonzero++;

				if (nonzero != 0) s += (nonzero == 0? "" : " ") + b[i] + (i == b.Length - 1? "" : " ");
			}

			return s;
		}

		public static int Wait { get; set; } = 60000;

		public static string Run(string target, string[] args, string testdata, ErrorReport r, bool detachoutput, out int exitcode)
		{
			try
			{
				var ass = new Process
				{
					StartInfo =
					{
						Arguments = args != null && args.Length > 0? string.Join(" ", args) : "",
						FileName = target,
						UseShellExecute = false,
						RedirectStandardError = true,
						RedirectStandardOutput = !detachoutput,
						RedirectStandardInput = true
					}
				};
                ass.Start();
                ass.StandardInput.WriteAsync(testdata);
				Console.WriteLine($"{target} start time: {ass.StartTime:G}");
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
											  $"{target} is taking to long to complete",
											  ErrorLevel.Error));
					}

				if (ass.ExitCode != 0)
				{
					r.Add(new StaticError(0, 0, $"{target} fail",
										  ErrorLevel.Error));
					if(!detachoutput)
						Console.WriteLine(ass.StandardOutput.ReadToEnd());
					Console.WriteLine(ass.StandardError.ReadToEnd());
					exitcode = ass.ExitCode;
					return "";
				}

				Console.WriteLine($"{target} exit time: {ass.ExitTime:G}");
				exitcode = ass.ExitCode;
				return detachoutput? "" : ass.StandardOutput.ReadToEnd();
			}
			catch (Exception f)
			{
				r.Add(new StaticError(0, 0, $"Run aborted: {f.Message}", ErrorLevel.Error));
				Debugger.Break();
				exitcode = -1000;
				return "";
			}
		}
	}
}
