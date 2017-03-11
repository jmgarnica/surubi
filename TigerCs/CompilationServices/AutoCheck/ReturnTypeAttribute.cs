using System;

namespace TigerCs.CompilationServices.AutoCheck
{
	[AttributeUsage(AttributeTargets.Property)]
	public class ReturnTypeAttribute : Attribute
	{
		public readonly RetrurnType Return;
		public ReturnTypeAttribute(RetrurnType Return)
		{
			this.Return = Return;
		}

		public string Dependency { get; set; }
	}
}
