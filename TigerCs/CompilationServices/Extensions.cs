using System;
using System.Collections.Generic;

namespace TigerCs.CompilationServices
{
	public static class Extensions
	{
		public static bool TryGetValue<T, R>(this IEnumerable<Tuple<T, R>> collection, T Key, out R value)
		{
			foreach (var pair in collection)
			{
				if (!pair.Item1.Equals(Key)) continue;
				value = pair.Item2;
				return true;
			}
			value = default(R);
			return false;
		}

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
	}
}
