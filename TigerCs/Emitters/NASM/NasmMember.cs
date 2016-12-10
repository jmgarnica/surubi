using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Emitters.NASM
{
	public abstract class NasmMember : IMember
	{
		protected NasmMember(NasmEmitterScope dscope, int sindex)
		{
			DeclaratingScope = dscope;
			DeclaringScopeIndex = sindex;
		}

		public int Levels(NasmEmitterScope accedingscope)
		{
			int levels = 0;
			while (accedingscope != DeclaratingScope)
			{
				levels++;
				accedingscope = accedingscope.Parent;
				if (accedingscope == null) return -1;
			}
			return levels;
		}

		public virtual void PutValueInRegister(Register gpr, FormatWriter fw, NasmEmitterScope accedingscope)
		{
			fw.WriteLine("");
			int levels = Levels(accedingscope);
			if(levels < 0) throw new NasmEmitterException("Unreachable Member");

			var reg = accedingscope.Lock.Locked(Register.EBX)? gpr : Register.EBX;

			fw.WriteLine(string.Format("mov {0}, {1}", reg, Register.EBP));

			for (int i = 0; i < levels; i++)
			{
				fw.WriteLine(string.Format("mov {0}, [{0}]", reg));
			}

			fw.WriteLine(string.Format("add {0}, {1}", reg, -(DeclaringScopeIndex + 1)*4));
			fw.WriteLine(string.Format("mov {0}, [{1}]", gpr, reg));
		}

		public virtual void StackBackValue(Register gpr, FormatWriter fw, NasmEmitterScope accedingscope)
		{
			fw.WriteLine("");
			int levels = Levels(accedingscope);
			if (levels < 0) throw new NasmEmitterException("Unreachable Member");

			bool stackback = false;
			var reg = accedingscope.Lock.LockGPR(Register.EBX);
			if (reg == null)
			{
				reg = gpr == Register.EBX ? Register.EDX : Register.EBX;
				stackback = true;
				fw.WriteLine("push " + reg.Value);
			}

			fw.WriteLine(string.Format("mov {0}, EBP", reg.Value));

			for (int i = 0; i < levels; i++)
			{
				fw.WriteLine(string.Format("mov {0}, [{0}]", reg.Value));
			}

			fw.WriteLine(string.Format("add {0}, {1}", reg.Value, -(DeclaringScopeIndex + 1) * 4));
			fw.WriteLine(string.Format("mov [{0}], {1}", reg.Value, gpr));

			if (stackback)
				fw.WriteLine("pop " + reg.Value);
			else accedingscope.Lock.Release(reg.Value);
		}

		public NasmEmitterScope DeclaratingScope { get; private set; }

		public int DeclaringScopeIndex { get; private set; }
	}
	
}
