using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Emitters.NASM
{
	public abstract class NasmHolder : NasmMember, IHolder
	{
		protected NasmHolder(NasmEmitterScope dscope, int sindex)
			: base(dscope, sindex)
		{ }

		public abstract bool Assignable { get; }

		public NasmEmitterScope DeclaratingScope { get; private set; }
		public int DeclaringScopeIndex { get; private set; }

		public NasmType EmitterType { get; protected set; }
	}

	public class NasmIntConst : NasmHolder
	{
		int value;

		public NasmIntConst(int value)
			: base(null, -1)
		{
			EmitterType = NasmType.Int;
			this.value = value;
		}

		public override bool Assignable
		{
			get { return false; }
		}

		public override void PutValueInRegister(Register gpr, FormatWriter fw, NasmEmitterScope accedingscope)
		{
			fw.WriteLine(string.Format("mov {0}, {1}", gpr, value));
		}
	}

	public class NasmStringConst : NasmHolder
	{
		string value;

		public NasmStringConst(string value, string label, string offset)
			: base(null, -1)
		{
			EmitterType = NasmType.String;
			this.value = value;
		}

		public override bool Assignable
		{
			get { return false; }
		}

		public override void PutValueInRegister(Register gpr, FormatWriter fw, NasmEmitterScope accedingscope)
		{
			fw.WriteLine(string.Format("mov {0}, {1}", gpr, value));
		}
	}
}
