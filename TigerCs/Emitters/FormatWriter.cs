using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TigerCs.Emitters
{
	public class FormatWriter
	{
		StringBuilder builder = new StringBuilder();
		ArrayList objects = new ArrayList();
		public int IndentationLevel { get; set; }

		public const char IndentChar = '	';
		public const char NewLine = '\n';

		public bool OnNewLine { get; private set; } = true;

		public int IndexOfFormat { get { return objects.Count; } }

		public void IncrementIndentation() { IndentationLevel++; }
		public void DecrementIndentation() { IndentationLevel = IndentationLevel <= 0 ? 0 : IndentationLevel - 1; }

		void Indent()
		{
			for (int i = 0; i < IndentationLevel; i++) builder.Append(IndentChar);
		}

		public static string Indent(string s, int indentationlevel)
		{
			StringBuilder b = new StringBuilder();
			b.Append(NewLine);
			for (int i = 0; i < indentationlevel; i++)
			{
				b.Append(IndentChar);
			}

			return (s[s.Length - 1] == NewLine? s.Substring(0,s.Length - 1) : s).Replace(NewLine.ToString(), b.ToString()) + NewLine;
        }

		public void Write(string text, params object[] toreplace)
		{
			if (text != null && text.Length != 0)
			{
				if (OnNewLine) Indent();
				if (text[text.Length - 1] == NewLine) OnNewLine = true;
				builder.Append(text);
				if (toreplace != null && toreplace.Length != 0) objects.AddRange(toreplace);
			}			
		}

		public void WriteLine(string text, params object[] toreplace)
		{
			if (!OnNewLine) builder.Append(NewLine);
			if (text != null && text.Length != 0)
			{
				Indent();
				builder.Append(text);
				if (toreplace != null && toreplace.Length != 0) objects.AddRange(toreplace);
				if (text[text.Length - 1] != NewLine) builder.Append(NewLine);
			}
			else builder.Append(NewLine);
			OnNewLine = true;
		}

		public void Flush(TextWriter w)
		{
			for (int i = 0; i < objects.Count; i++)
			{
				if (objects[i] is Func<object>) objects[i] = ((Func<object>)objects[i])();
			}
			w.Write(string.Format(builder.ToString(), objects.ToArray()));
		}

		public string Flush()
		{
			for (int i = 0; i < objects.Count; i++)
			{
				if (objects[i] is Func<object>) objects[i] = ((Func<object>)objects[i])();
			}
			return string.Format(builder.ToString(), objects);
		}
	}

}
