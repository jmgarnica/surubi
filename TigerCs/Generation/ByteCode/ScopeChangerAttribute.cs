using System;

namespace TigerCs.Generation.ByteCode
{
	/// <summary>
	/// Marks a method as modifying the CurrentScope property of a TigerEmitter.
	/// For documentation.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	public class ScopeChangerAttribute : Attribute
	{
		public string Reason { get; set; }
		public string ScopeName { get; set; }
	}
}
