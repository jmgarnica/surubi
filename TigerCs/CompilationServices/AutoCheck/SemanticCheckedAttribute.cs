using System;

namespace TigerCs.CompilationServices.AutoCheck
{
	[AttributeUsage(AttributeTargets.Property)]
	public class SemanticCheckedAttribute : Attribute
	{
		public int CheckOrder { get; set; } = 0;
		public OnError Action { get; set; } = OnError.StopAfterTest;
		public bool NestedScope { get; set; } = false;
		public string FailMessage { get; set; } = "";


		public ExpectedType Expected { get; set; }
		public string Dependency { get; set; }
	}
}
