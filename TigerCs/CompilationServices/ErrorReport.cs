using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace TigerCs.CompilationServices
{
	using System.Linq;

	public class ErrorReport : IEnumerable<StaticError>
	{
		readonly List<StaticError> report;
		public event Action<StaticError> CriticalError, Error, Warning, Info;

		public ErrorReport()
		{
			report = new List<StaticError>();
		}

		public int Count()
		{
			return (from e in report
			        where e.Level != ErrorLevel.Info && e.Level != ErrorLevel.Warning
			        select e).
				Count();
		}

		public void Add(StaticError error)
		{
			report.Add(error);
			switch (error.Level)
			{
				case ErrorLevel.Info:
					Info?.Invoke(error);
					break;
				case ErrorLevel.Warning:
					Warning?.Invoke(error);
					break;
				case ErrorLevel.Error:
					Error?.Invoke(error);
					break;
				case ErrorLevel.Internal:
					CriticalError?.Invoke(error);
					if (Debugger.IsAttached)
						Debugger.Break();
					break;
				default:
					Info?.Invoke(error);
					break;
			}
		}

		public IEnumerator<StaticError> GetEnumerator()
		{
			return report.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void IncompleteMemberInitialization(string source = null, int line = 0, int column = 0)
			=> Add(new StaticError(line, column, "Incomple member initialization", ErrorLevel.Internal, source));
	}

	public struct StaticError
	{
		public string ErrorMessage;
		public ErrorLevel Level;
		public string SourceCode;
		public int Line;
		public int Column;

		public StaticError(int line, int colunm, string error, ErrorLevel level, string source = null)
		{
			ErrorMessage = error;
			Level = level;
			SourceCode = source;
			Line = line;
			Column = colunm;
		}

		public override string ToString()
		{
			string format = "<{3}> [{0}:{1}] {2}";
			if (!string.IsNullOrWhiteSpace(SourceCode))
				format += '\n'.ToString() + "{4}";

			return string.Format(format, Line, Column, ErrorMessage, Level, SourceCode);
		}
	}

	public enum ErrorLevel
	{
		/// <summary>
		/// Compiler information
		/// </summary>
		Info,

		/// <summary>
		/// Possible error
		/// </summary>
		Warning,

		/// <summary>
		/// Error from user code
		/// </summary>
		Error,

		/// <summary>
		/// Unspected error, likely from developer code
		/// </summary>
		Internal,
	}
}
