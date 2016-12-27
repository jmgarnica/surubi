namespace TigerCs.Generation
{
	using System;

	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
	public class ReleaseAttribute : Attribute
	{
		public ReleaseAttribute(bool collection = false)
		{
			Collection = collection;
		}

		public bool Collection { get; }
	}
}