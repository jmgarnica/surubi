using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Emitters.NASM
{
	public class NasmFunction : NasmMember, IFunction<NasmType, NasmFunction>
	{

		public NasmFunction(NasmEmitterScope dscope, int sindex)
			: base(dscope, sindex)
		{ }

		public override void PutValueInRegister(Register gpr, FormatWriter fw, NasmEmitterScope accedingscope)
		{
			throw new NotImplementedException();
		}

		public string Name { get; private set; }
		public bool Bounded
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public NasmType Return
		{
			get
			{
				throw new NotImplementedException();
			}
		}
	}
}
