using System;

namespace TigerCs.CompilationServices.AutoCheck
{
	[AttributeUsage(AttributeTargets.Property)]
	public class NotNullAttribute : Attribute
	{
		public object[] InvalidValues { get; set; }
	}
}
