using System;

namespace TigerCs.CompilationServices
{

	[AttributeUsage(AttributeTargets.Property)]
	public sealed class InArgumentAttribute : Attribute
	{
		public object DefaultValue { get; set; }
		public string Comment { get; set; }
		public string ConsoleShortName { get; set; }
	}
}
