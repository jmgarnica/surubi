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
	}
}
