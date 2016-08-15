using System.Collections;
using System.Collections.Generic;

namespace TigerCs.Generation
{
	public class ErrorReport : IEnumerable<TigerStaticError>
	{
		List<TigerStaticError> report;

		public ErrorReport()
		{
			report = new List<TigerStaticError>();
		}

		public void Add(int line, int collum, TigerStaticError error)
		{
			report.Add(error);
		}

		public IEnumerator<TigerStaticError> GetEnumerator()
		{
			return report.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	public struct TigerStaticError
	{
		public string ErrorMessage;
		public ErrorLevel Level;
		public string SourceCode;
		public int Line;
		public int Colunm;
		   
		public TigerStaticError(int line, int colunm, string error, ErrorLevel level, string source = null)
		{
			ErrorMessage = error;
			Level = level;
			SourceCode = source;
			Line = line;
			Colunm = colunm;
		}

		public override string ToString()
		{
			string format = "<{3}> [{0}:{1}] {2}";
			if (!string.IsNullOrWhiteSpace(SourceCode))
				format += '\n'.ToString() + "{4}";

			return string.Format(format, Line, Colunm, ErrorMessage, Level, SourceCode);
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
