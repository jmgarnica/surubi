using System;

namespace TigerCs.Emitters
{

	[Serializable]
	public abstract class EmitterErrorException : Exception
	{
		protected EmitterErrorException() { }
		protected EmitterErrorException(string message) : base(message) { }
		protected EmitterErrorException(string message, Exception inner) : base(message, inner) { }
		protected EmitterErrorException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context)
		{ }
	}
}
