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
		NasmEmitter bound;
		bool checkupperbound;

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
		public NasmReference(NasmHolder h, int offset, NasmEmitter bound, NasmEmitterScope dscope = null, WordSize size = WordSize.DWord, bool checkupperbound = false)
			:base(dscope ?? h.DeclaratingScope, 0)
		{ 
			H = h;
			this.size = size;
			this.bound = bound;
			this.checkupperbound = checkupperbound;
			this.offset = offset; //+4 see NasmEmitter.InstrSize <remarks>[second mode]</remarks>
		}

		public override void PutValueInRegister(Register gpr, FormatWriter fw, NasmEmitterScope accedingscope)
		{
			H.PutValueInRegister(gpr, fw, accedingscope);
			if (offset > 0 && checkupperbound)
			{
				//<error catch>
				Guid doit = bound.g.GNext();
				Guid ndoit = bound.g.GNext();

				fw.WriteLine(string.Format("cmp dword {0}, [{1}]", offset, gpr));
				fw.WriteLine(string.Format("jge _{0}", doit.ToString("N")));
				fw.WriteLine(string.Format("jmp _{0}", ndoit.ToString("N")));

				fw.WriteLine(string.Format("_{0}:", doit.ToString("N")));
				NasmEmitter.EmitError(fw, accedingscope, bound, 1, "Index out of range");

				fw.WriteLine(string.Format("_{0}:", ndoit.ToString("N")));
				//</error catch>
			}
			fw.WriteLine(string.Format("add {0}, {1}", gpr, 4 + offset * (int)size));
			fw.WriteLine(string.Format("mov {0}, [{1}]", size == WordSize.Byte ? gpr.ByteVersion() : gpr, gpr));
			if (size == WordSize.Byte)
				fw.WriteLine(string.Format("movzx {0}, {1}", gpr, gpr.ByteVersion()));
        }

		public override void StackBackValue(Register gpr, FormatWriter fw, NasmEmitterScope accedingscope)
		{
			bool stackback = false;
			var reg = accedingscope.Lock.LockGPR(Register.EBX);
			if (reg == null)
			{
				reg = gpr == Register.EBX ? Register.EDX : Register.EBX;
				stackback = true;
				fw.WriteLine("push " + reg.Value);
			}

			H.PutValueInRegister(reg.Value, fw, accedingscope);
			//<error catch>
			if (offset > 0 && checkupperbound)
			{
				bool stackback2 = false;
				var reg2 = accedingscope.Lock.LockGPR(Register.EDX);
				if (reg2 == null)
				{
					reg2 = reg.Value == Register.EDX ? Register.EAX : Register.EDX;
					stackback2 = true;
					fw.WriteLine("push " + reg2.Value);
				}
				
				Guid doit = bound.g.GNext();
				Guid ndoit = bound.g.GNext();
				fw.WriteLine(string.Format("mov {0}, {1}", reg2.Value, offset));
				fw.WriteLine(string.Format("cmp {0}, [{1}]", reg2.Value, reg.Value));
				fw.WriteLine(string.Format("jge _{0}", doit.ToString("N")));
				fw.WriteLine(string.Format("jmp _{0}", ndoit.ToString("N")));

				fw.WriteLine(string.Format("_{0}:", doit.ToString("N")));
				NasmEmitter.EmitError(fw, accedingscope, bound, 1, "Index out of range");

				fw.WriteLine(string.Format("_{0}:", ndoit.ToString("N")));

				if (stackback2)
					fw.WriteLine("pop " + reg2.Value);
				else accedingscope.Lock.Release(reg2.Value);
			}
			//</error catch>
			//<new code>
			fw.WriteLine(string.Format("add {0}, {1}", reg.Value, 4 + offset * (int)size));
			fw.WriteLine(string.Format("mov [{0}], {1}", reg.Value, size == WordSize.Byte? gpr.ByteVersion() : gpr));
			//</new code>
			if (stackback)
				fw.WriteLine("pop " + reg.Value);
			else accedingscope.Lock.Release(reg.Value);
			fw.WriteLine("");
		}
	}
}
