using System;

namespace TigerCs.CompilationServices.AutoCheck
{
	[AttributeUsage(AttributeTargets.Property)]
	public class ReturnTypeAttribute : Attribute
	{
		public readonly ExpectedType Return;
		public ReturnTypeAttribute(ExpectedType Return)
		{
			this.Return = Return;
		}

		public string Dependency { get; set; }

		public OnError Action { get; set; } = OnError.ErrorButNotStop;
	}
}
