using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Emitters.NASM
{
	public class NasmType : NasmMember, IType<NasmType, NasmFunction>
	{
		public static readonly NasmType Int;
		public static readonly NasmType String;

		public NasmType(NasmEmitterScope dscope, int sindex)
			: base(dscope, sindex)
		{ }

		public NasmFunction Allocation
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public bool Array
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public NasmFunction ArrayAccess
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public NasmFunction Deallocator
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public override void PutValueInRegister(Register gpr, FormatWriter fw, NasmEmitterScope accedingscope)
		{
			throw new NotImplementedException();
		}
	}
}
