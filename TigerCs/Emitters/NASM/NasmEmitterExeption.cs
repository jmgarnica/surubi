using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TigerCs.Emitters.NASM
{

	[Serializable]
	public class NasmEmitterException : EmitterErrorException
	{
		public NasmEmitterException() { }
		public NasmEmitterException(string message) : base(message) { }
		public NasmEmitterException(string message, Exception inner) : base(message, inner) { }
		protected NasmEmitterException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context)
		{ }
	}
}
