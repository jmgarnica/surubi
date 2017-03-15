using System;

namespace TigerCs.CompilationServices.AutoCheck
{
	[AttributeUsage(AttributeTargets.Property)]
	public class NotNullAttribute : Attribute
	{
		public NotNullAttribute(params object[] invalidvalues)
		{
			InvalidValues = invalidvalues;
		}

		public readonly object[] InvalidValues;
	}
}
