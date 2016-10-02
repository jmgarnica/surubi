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
		int indentationlevel = 0;

		public const char IndentChar = '	';
		public const char NewLine = '\n';

		public bool OnNewLine { get; private set; } = true;

		public int IndexOfFormat { get { return objects.Count; } }

		public void Write(string text, params object[] toreplace)
		{
			if (text != null && text.Length != 0)
			{
				if (text[text.Length] == NewLine) OnNewLine = true;
				builder.Append(text);
				if (toreplace != null && toreplace.Length != 0) objects.AddRange(toreplace);
			}			
		}

		public void WriteLine(string text, params object[] toreplace)
		{
			if (!OnNewLine) builder.Append(NewLine);
			if (text != null && text.Length != 0)
			{
				builder.Append(text);
				if (toreplace != null && toreplace.Length != 0) objects.AddRange(toreplace);
				if (text[text.Length] != NewLine) builder.Append(NewLine);
			}
			else builder.Append(NewLine);
			OnNewLine = true;
		}

		public void Flush(StreamWriter w)
		{
			for (int i = 0; i < objects.Count; i++)
			{
				if (objects[i] is Func<object>) objects[i] = ((Func<object>)objects[i])();
			}
			w.Write(string.Format(builder.ToString(), objects));
		}
	}

}
