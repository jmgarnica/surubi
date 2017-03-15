using System;

namespace TigerCs.CompilationServices.AutoCheck
{
	[AttributeUsage(AttributeTargets.Property)]
	public class SemanticCheckedAttribute : Attribute
	{
		public int CheckOrder { get; set; } = 0;
		public bool IgnoreFail { get; set; } = false;
		public bool NestedScope { get; set; } = false;
		public string FailMessage { get; set; } = "";


		public ExpectedType Expected { get; set; }
		public string Dependency { get; set; }
	}
}
