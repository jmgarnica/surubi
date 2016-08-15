using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TigerCs.Generation.Semantic.Scopes
{
	public class TigerScope
	{
		public TigerScope Parent { get; protected set; }
		public List<TigerScope> Children { get; protected set; }
		protected Dictionary<string, MemberInfo> Namespace { get; set; }


	}
}
