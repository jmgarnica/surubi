using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TigerCs.Emitters
{
	public interface IWriter
	{
		void Write(string text);

		void WriteLine();
		void WriteLine(string line);

		void Indent();
		void Dedent();
	}

	public interface IDelayedWriter : IWriter
	{
		void FlushOver(IWriter w);
	}

	public abstract class MonoTrackWriter : IWriter
	{
		int indentationlevel = 0;
		public char indentationsymbol = '	';

		StringBuilder b = new StringBuilder();

		public void Dedent()
		{
			if (indentationlevel > 0) indentationlevel--;
		}

		public void Indent()
		{
			indentationlevel++;
		}

		public void Write(string text)
		{
			b.Clear();
			b.Append(indentationsymbol, indentationlevel);
			b.Append(text, 0, text.Length);
			write(text);
		}

		public abstract void WriteLine();

		public void WriteLine(string line)
		{
			b.Clear();
			b.Append(indentationsymbol, indentationlevel);
			b.Append(line, 0, line.Length);
			writeline(line);
		}

		public abstract void write(string text);

		public abstract void writeline(string line);

	}

	public class TextWriterWrapper : MonoTrackWriter
	{
		TextWriter w;
		public TextWriterWrapper(TextWriter w)
		{
			this.w = w;
		}

		public override void write(string text)
		{
			w.Write(text);
		}
		public override void WriteLine()
		{
			w.WriteLine();
		}
		public override void writeline(string line)
		{
			w.WriteLine(line);
		}
	}

	public class StringWriter : MonoTrackWriter, IDelayedWriter
	{
		StringBuilder b;
		public string linechange = new string(new char[] { (char)13, (char)10 });

		public StringWriter()
		{
			b = new StringBuilder();
		}

		public void FlushOver(IWriter w)
		{
			w.Write(b.ToString());
			b.Clear();
		}

		public override void write(string text)
		{
			b.Append(text, 0, text.Length);
		}

		public override void WriteLine()
		{
			b.Append(linechange, 0, 2);
		}

		public override void writeline(string line)
		{
			b.Append(line + linechange, 0, line.Length + 2);
		}
	}

	public class MultiTrackWriter : IWriter
	{
		IWriter main;
		List<IWriter> front, tail;

		public MultiTrackWriter(IWriter main)
		{
			this.main = main;
			front = new List<IWriter>();
			tail = new List<IWriter>();
		}

		public void Write(string text)
		{
			main.Write(text);
		}

		public void Write(string text, int track)
		{
			this[track].Write(text);
		}

		public void WriteLine()
		{
			main.WriteLine();
		}

		public void WriteLine(int track)
		{
			this[track].WriteLine();
		}

		public void WriteLine(string line)
		{
			main.WriteLine(line);
		}

		public void WriteLine(string line, int track)
		{
			this[track].WriteLine(line);
		}

		public void Indent()
		{
			main.Indent();
		}

		public void Indent(int track)
		{
			this[track].Indent();
		}

		public void Dedent()
		{
			main.Dedent();
		}

		public void Dedent(int track)
		{
			this[track].Dedent();
		}

		public int SpawnWriter(IDelayedWriter w, bool front = true)
		{
			if (front)
			{
				this.front.Add(w);
				return this.front.Count;
			}
			tail.Add(w);
			return -tail.Count;
		}

		public void OrderedFlush()
		{
			foreach (var item in front)
				((IDelayedWriter)item).FlushOver(main);

			for (int i = tail.Count - 1; i >= 0; i--)
			{
				((IDelayedWriter)tail[i]).FlushOver(main);
			}
		}

		IWriter this[int index]
		{
			get
			{
				if (index == 0)
				{
					return main;
				}
				else if (index > 0)
				{
					index--;
					return front[index];
				}
				else
				{
					index = -index - 1;
					return tail[index];
				}
			}

			set
			{
				if (index == 0)
				{
					main = value;
				}
				else if (index > 0)
				{
					index--;
					front[index] = value;
				}
				else
				{
					index = -index - 1;
					tail[index] = value;
				}
			}
		}
	}

	public class ParallelTrackWriter : IWriter
	{
		List<IWriter> w;

		public ParallelTrackWriter(IWriter w)
		{
			this.w = new List<IWriter>() { w };
		}

		public void AddWriter(IWriter w)
		{
			this.w.Add(w);
		}

		public void Dedent()
		{
			w.ForEach((i) => i.Dedent());
		}

		public void Indent()
		{
			w.ForEach((i) => i.Indent());
		}

		public void Write(string text)
		{
			w.ForEach((i) => i.Write(text));
		}

		public void WriteLine()
		{
			w.ForEach((i) => i.WriteLine());
		}

		public void WriteLine(string line)
		{
			w.ForEach((i) => i.WriteLine(line));
		}
	}
}
