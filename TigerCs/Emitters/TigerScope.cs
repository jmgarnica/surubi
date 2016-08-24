using System.Collections.Generic;
using TigerCs.Generation.Semantic;

namespace TigerCs.Emitters
{
	public class TigerScope
	{
		public const string ScopeNameSeparator = "_";
		public string ScopeTag { get; set; }
		public TigerScope Parent { get; protected set; } = null;
		public List<TigerScope> Children { get; protected set; } = new List<TigerScope>();
		protected Dictionary<string, MemberInfo> Namespace { get; set; } = new Dictionary<string, MemberInfo>();
		public bool ContainsTypeDefinitions { get; protected set; } = false;

		public HashSet<string> RegistedLabel { get; protected set; } = new HashSet<string>();
		public HashSet<string> PendingLabels { get; protected set; } = new HashSet<string>();
		public HashSet<string> UnusedLabels { get; protected set; } = new HashSet<string>();

		public bool DeclareMember(string name, MemberInfo member)
		{
			MemberInfo existent;
			if (Reachable(name, out existent)) return false;

			Namespace[name] = member;
			if (member is TypeInfo) ContainsTypeDefinitions = true;
			return true;
		}

		public bool Reachable(string name, out MemberInfo member)
		{
			var current = this;
			do
			{
				if (current.Namespace.TryGetValue(name, out member)) return true;
				current = current.Parent;
			}
			while (current != null);
			return false;
		}
	}
}
