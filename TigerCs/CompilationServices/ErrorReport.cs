using System;
using System.Collections;
using System.Collections.Generic;

namespace TigerCs.CompilationServices
{
	public class ErrorReport : IEnumerable<TigerStaticError>
	{
		List<TigerStaticError> report;
		public event Action<TigerStaticError> CriticalError, Error, Warning, Info;

		public ErrorReport()
		{
			report = new List<TigerStaticError>();
		}

		public void Add(TigerStaticError error)
		{
			report.Add(error);
			switch (error.Level)
			{
				case ErrorLevel.Info:
					if (Info != null) Info(error);
					break;
				case ErrorLevel.Warning:
					if (Info != null) Info(error);
					break;
				case ErrorLevel.Error:
					if (Info != null) Info(error);
					break;
				case ErrorLevel.Critical:
					if (Info != null) Info(error);
					break;
				default:
					if (Info != null) Info(error);
					break;
			}
		}

		public IEnumerator<TigerStaticError> GetEnumerator()
		{
			return report.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void IncompleteMemberInitialization(string source = null)
			=> Add(new TigerStaticError(0, 0, "Incomple member initialization", ErrorLevel.Critical, source));			
	}

	public struct TigerStaticError
	{
		public string ErrorMessage;
		public ErrorLevel Level;
		public string SourceCode;
		public int Line;
		public int Column;
		   
		public TigerStaticError(int line, int colunm, string error, ErrorLevel level, string source = null)
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
		Info,
		Warning,
		Error,
		Critical,
	}
}
