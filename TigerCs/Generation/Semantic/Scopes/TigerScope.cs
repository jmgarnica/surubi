using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TigerCs.Generation.Semantic.Scopes
{
	public class TigerScope
	{
		public TigerScope Parent { get; protected set; } = null;
		public List<TigerScope> Children { get; protected set; } = new List<TigerScope>();
		protected Dictionary<string, MemberInfo> Namespace { get; set; } = new Dictionary<string, MemberInfo>();
		public bool ContainsTypeDefinitions { get; protected set; } = false;

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
