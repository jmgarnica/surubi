using System;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Emitters.NASM
{
	public class NasmHolder : NasmMember, IHolder
	{
		public NasmHolder(NasmEmitterScope dscope, int sindex)
			: base(dscope, sindex)
		{ }

		public virtual bool Assignable { get { return true; } }
    }

	public class NasmIntConst : NasmHolder
	{
		int value;

		public NasmIntConst(int value)
			: base(null, -1)
		{
			this.value = value;
		}

		public override bool Assignable
		{
			get { return false; }
		}

		public override void PutValueInRegister(Register gpr, FormatWriter fw, NasmEmitterScope accedingscope)
		{
			if (value != 0) fw.WriteLine(string.Format("mov {0}, {1}", gpr, value));
			else fw.WriteLine(string.Format("xor {0}, {0}", gpr));
		}

		public override void StackBackValue(Register gpr, FormatWriter fw, NasmEmitterScope accedingscope)
		{
			throw new NasmEmitterException("Constant values cant be assigned");
		}
	}

	public class NasmStringConst : NasmHolder
	{
		string value;
		string label;
		public readonly int offset;

		public NasmStringConst(string value, string label, int offset)
			: base(null, -1)
		{
			this.value = value;
			this.label = label;
			this.offset = offset;
		}

		public override bool Assignable
		{
			get { return false; }
		}

		public override void PutValueInRegister(Register gpr, FormatWriter fw, NasmEmitterScope accedingscope)
		{
			fw.WriteLine(string.Format("mov {0}, {1}", gpr, label));
			fw.WriteLine(string.Format("{2}add {0}, {1}", gpr, offset, offset != 0 ? "" : ";"));
		}

		public override void StackBackValue(Register gpr, FormatWriter fw, NasmEmitterScope accedingscope)
		{
			throw new NasmEmitterException("Constant values cant be assigned");
		}
	}

	public class NasmReference : NasmHolder
	{
		NasmHolder H;
		int offset;
		WordSize size;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="h"></param>
		/// <param name="offset"></param>
		/// <param name="dscope">
		/// 
		/// </param>
		/// <param name="size">
		/// Only on byte an dword mode
		/// </param>
		public NasmReference(NasmHolder h, int offset, NasmEmitterScope dscope = null, WordSize size = WordSize.DWord)
			:base(dscope ?? h.DeclaratingScope, 0)
		{ 
			H = h;
			this.size = size;
			this.offset = 4 + offset * (int)size; //+4 see NasmEmitter.InstrSize <remarks>[second mode]</remarks>
		}

		public override void PutValueInRegister(Register gpr, FormatWriter fw, NasmEmitterScope accedingscope)
		{
			H.PutValueInRegister(gpr, fw, accedingscope);
			fw.WriteLine(string.Format("add {0}, {1}", gpr, offset));
			fw.WriteLine(string.Format("mov {0}, [{1}]", size == WordSize.Byte ? gpr.ByteVersion() : gpr, gpr));
			if (size == WordSize.Byte)
				fw.WriteLine(string.Format("movzx {0}, {1}", gpr, gpr.ByteVersion()));
        }

		public override void StackBackValue(Register gpr, FormatWriter fw, NasmEmitterScope accedingscope)
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

			for (int i = 0; i < levels; i++)
			{
				fw.WriteLine(string.Format("mov {0}, [{0}]", reg.Value));
			}

			fw.WriteLine(string.Format("add {0}, {1}", reg.Value, -(DeclaringScopeIndex + 1) * 4));
			fw.WriteLine(string.Format("mov {0}, [{0}]", reg.Value));
			//<new code>
			fw.WriteLine(string.Format("add {0}, {1}", reg.Value, offset));
			fw.WriteLine(string.Format("mov [{0}], {1}", reg.Value, size == WordSize.Byte? gpr.ByteVersion() : gpr));
			//</new code>
			if (stackback)
				fw.WriteLine("pop " + reg.Value);
			else accedingscope.Lock.Release(reg.Value);
			fw.WriteLine("");
		}
	}
}
