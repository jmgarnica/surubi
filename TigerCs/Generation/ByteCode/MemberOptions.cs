using System;

namespace TigerCs.Generation.ByteCode
{
	[Flags]
	public enum HolderOptions
	{
		Default = 0,
		Global = 1,
		Trapped = 2
	}

	[Flags]
	public enum FunctionOptions
	{
		Default = 0,
		Global = 1,
		Delegate = 2
	}
}
