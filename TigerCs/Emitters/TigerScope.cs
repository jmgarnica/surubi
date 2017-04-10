using System.Collections.Generic;
using TigerCs.Generation;
using TigerCs.CompilationServices;

namespace TigerCs.Emitters
{
	public class SemanticScope
	{
		public const string ScopeNameSeparator = "_";

		public SemanticScope Parent { get; set; } = null;

		public IDictionary<string, MemberDefinition> Namespace { get; protected set; } = new Dictionary<string, MemberDefinition>();
		public IDictionary<string, MemberInfo> Closure { get; set; }
		public bool ContainsTypeDefinitions { get; set; } = false;
		public object[] Descriptors { get; set; } = null;


	}

	public class GenerationScope
	{
		public GenerationScope Parent { get; set; } = null;
		public string ScopeTag { get; set; }
		public List<GenerationScope> Children { get; protected set; } = new List<GenerationScope>();
		public HashSet<string> RegistedLabel { get; protected set; } = new HashSet<string>();
		public HashSet<string> PendingLabels { get; protected set; } = new HashSet<string>();
		public HashSet<string> UnusedLabels { get; protected set; } = new HashSet<string>();
	}
}
