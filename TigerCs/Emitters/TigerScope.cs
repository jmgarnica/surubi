using System.Collections.Generic;
using TigerCs.Generation;
using TigerCs.CompilationServices;

namespace TigerCs.Emitters
{
	

	public class TigerSemanticScope
	{
		public const string ScopeNameSeparator = "_";

		public TigerSemanticScope Parent { get; set; } = null;

		public Dictionary<string, MemberDefinition> Namespace { get; protected set; } = new Dictionary<string, MemberDefinition>();
		public Dictionary<string, MemberInfo> Closure { get; set; }
		public bool ContainsTypeDefinitions { get; set; } = false;
		public object[] Descriptors { get; set; } = null;


	}

	public class TigerGenerationScope
	{
		public TigerGenerationScope Parent { get; set; } = null;
		public string ScopeTag { get; set; }
		public List<TigerGenerationScope> Children { get; protected set; } = new List<TigerGenerationScope>();
		public HashSet<string> RegistedLabel { get; protected set; } = new HashSet<string>();
		public HashSet<string> PendingLabels { get; protected set; } = new HashSet<string>();
		public HashSet<string> UnusedLabels { get; protected set; } = new HashSet<string>();
	}
}
