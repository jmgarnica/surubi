using System;
using TigerCs.Generation.ByteCode;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace TigerCs.Emitters.NASM
{

	public class NasmHolder : NasmMember, IHolder
	{
		public NasmHolder(NasmEmitter bound, NasmEmitterScope dscope, int sindex)
			: base(bound, dscope, sindex)
		{ }

		public virtual bool Assignable { get { return true; } }
		public virtual int OnClosure { get; set; } = -1;

		public override void PutValueInRegister(Register gpr, FormatWriter fw, NasmEmitterScope accedingscope)
		{
			base.PutValueInRegister(gpr, fw, accedingscope);
			if (OnClosure < 0) return;

			fw.WriteLine($"mov {gpr}, [{gpr + (OnClosure > 0? $" - {OnClosure * 4}": "")}]");
		}

		public override void StackBackValue(Register gpr, FormatWriter fw, NasmEmitterScope accedingscope)
		{
			if (OnClosure < 0)
			{
				base.StackBackValue(gpr,fw,accedingscope);
				return;
			}

			var reg = accedingscope.Lock.LockGPR(Register.EDX);
			bool stackback = reg == null;
			if (stackback)
			{
				reg = gpr != Register.EDX? Register.EDX : Register.EBX;
				fw.WriteLine($"push {reg}");
			}
			base.PutValueInRegister(reg.Value, fw, accedingscope);
			if(OnClosure > 0)
				fw.WriteLine($"add {reg}, {-OnClosure * 4}");
			fw.WriteLine($"mov [{reg}], {gpr}");

			if (stackback) fw.WriteLine($"pop {reg}");
			else accedingscope.Lock.Release(reg.Value);
		}
	}

	public class NasmIntConst : NasmHolder
	{
		public readonly int value;

		public NasmIntConst(NasmEmitter bound, int value)
			: base(bound, null, -1)
		{
			this.value = value;
		}

		public override bool Assignable
		{
			get { return false; }
		}

		public override void PutValueInRegister(Register gpr, FormatWriter fw, NasmEmitterScope accedingscope)
		{
			if (value != 0) fw.WriteLine($"mov {gpr}, {value}");
			else fw.WriteLine(string.Format("xor {0}, {0}", gpr));
		}

		public override void StackBackValue(Register gpr, FormatWriter fw, NasmEmitterScope accedingscope)
		{
			throw new NasmEmitterException("Constant values cant be assigned");
		}
	}

	public class NasmStringConst : NasmHolder
	{
		[SuppressMessage("ReSharper", "NotAccessedField.Local")]
		string value;

		readonly string label;
		public readonly int offset;

		public NasmStringConst(NasmEmitter bound, string value, string label, int offset)
			: base(bound, null, -1)
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
			fw.WriteLine($"mov {gpr}, {label}");
			fw.WriteLine(string.Format("{2}add {0}, {1}", gpr, offset, offset != 0 ? "" : ";"));
		}

		public override void StackBackValue(Register gpr, FormatWriter fw, NasmEmitterScope accedingscope)
		{
			throw new NasmEmitterException("Constant values cant be assigned");
		}
	}

	public class NasmReference : NasmHolder
	{
		readonly NasmHolder H;
		readonly int offset;
		readonly WordSize size;
		readonly bool checkupperbound;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="h"></param>
		/// <param name="offset"></param>
		/// <param name="bound"></param>
		/// <param name="dscope">
		/// 
		/// </param>
		/// <param name="size">
		/// Only on byte an dword mode
		/// </param>
		/// <param name="checkupperbound"></param>
		public NasmReference(NasmHolder h, int offset, NasmEmitter bound, NasmEmitterScope dscope = null, WordSize size = WordSize.DWord, bool checkupperbound = false)
			:base(bound, dscope ?? h.DeclaringScope, 0)
		{
			H = h;
			this.size = size;

			this.checkupperbound = checkupperbound;
			this.offset = offset; //+4 see NasmEmitter.InstrSize <remarks>[second mode]</remarks>
		}

		public override void PutValueInRegister(Register gpr, FormatWriter fw, NasmEmitterScope accedingscope)
		{
			//TODO: aceptar array de direcciones, a[3,2,3] = ((a[3])[2])[3] = a.item4.item3.item4
			H.PutValueInRegister(gpr, fw, accedingscope);
			Guid passnull = bound.g.GNext();

			fw.WriteLine($"cmp {gpr}, {NasmEmitter.Null}");
			fw.WriteLine($"jne _{passnull:N}");

			NasmEmitter.EmitError(fw, accedingscope, bound, 3, "Null Refernce");

			fw.WriteLine($"_{passnull:N}:");
			if (offset > 0 && checkupperbound)
			{
				//<error catch>
				Guid IOEexcept = bound.g.GNext();
				Guid code = bound.g.GNext();

				bool stackback = false;
				var reg = accedingscope.Lock.LockGPR(Register.EDX);
				if (reg == null)
				{
					reg = Register.EDX != gpr ? Register.EDX : Register.EBX;
					fw.WriteLine($"push {reg.Value}");
					stackback = true;
				}

				fw.WriteLine($"mov {reg.Value}, {offset}");
				fw.WriteLine($"cmp {reg.Value}, [{gpr}]");

				if (stackback) fw.WriteLine($"pop {reg.Value}");
				else accedingscope.Lock.Release(reg.Value);

				fw.WriteLine($"jge _{IOEexcept:N}");
				fw.WriteLine($"jmp _{code:N}");

				fw.WriteLine($"_{IOEexcept:N}:");
				NasmEmitter.EmitError(fw, accedingscope, bound, 1, "Index out of range");

				fw.WriteLine($"_{code:N}:");
				//</error catch>
			}
			fw.WriteLine($"add {gpr}, {4 + offset * (int)size}");
			fw.WriteLine($"mov {(size == WordSize.Byte? gpr.ByteVersion() : gpr)}, [{gpr}]");
			if (size == WordSize.Byte)
				fw.WriteLine($"movzx {gpr}, {gpr.ByteVersion()}");
        }

		public override void StackBackValue(Register gpr, FormatWriter fw, NasmEmitterScope accedingscope)
		{
			fw.WriteLine("");
			bool stackback = false;
			var reg = accedingscope.Lock.LockGPR(Register.EBX);
			if (reg == null)
			{
				reg = gpr == Register.EBX ? Register.EDX : Register.EBX;
				stackback = true;
				fw.WriteLine("push " + reg.Value);
			}

			H.PutValueInRegister(reg.Value, fw, accedingscope);
			Guid passnull = bound.g.GNext();

			fw.WriteLine($"cmp {reg}, {NasmEmitter.Null}");
			fw.WriteLine($"jne _{passnull:N}");

			NasmEmitter.EmitError(fw, accedingscope, bound, 3, "Null Refernce");

			fw.WriteLine($"_{passnull:N}:");
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
				fw.WriteLine($"mov {reg2.Value}, {offset}");
				fw.WriteLine($"cmp {reg2.Value}, [{reg.Value}]");
				fw.WriteLine($"jge _{doit:N}");
				fw.WriteLine($"jmp _{ndoit:N}");

				fw.WriteLine($"_{doit:N}:");
				NasmEmitter.EmitError(fw, accedingscope, bound, 1, "Index out of range");

				fw.WriteLine($"_{ndoit:N}:");

				if (stackback2)
					fw.WriteLine("pop " + reg2.Value);
				else accedingscope.Lock.Release(reg2.Value);
			}
			//</error catch>
			//<new code>
			fw.WriteLine($"add {reg.Value}, {4 + offset * (int)size}");
			fw.WriteLine($"mov [{reg.Value}], {(size == WordSize.Byte? gpr.ByteVersion() : gpr)}");
			//</new code>
			if (stackback)
				fw.WriteLine("pop " + reg.Value);
			else accedingscope.Lock.Release(reg.Value);
		}
	}

	public class NasmDelegate : NasmHolder
	{
		public NasmFunction Function { get; }

		public NasmDelegate(NasmEmitter bound, NasmEmitterScope dscope, int sindex)
			: base(bound, dscope, sindex)
		{
			Function = new NasmFunction(dscope, sindex, bound, "internal");
		}
	}

	class NasmRegisterHolder : NasmHolder
	{
		readonly Register r;
		public NasmRegisterHolder(NasmEmitter bound, Register r)
			:base(bound,null, -1)
		{
			this.r = r;
		}

		public override void PutValueInRegister(Register gpr, FormatWriter fw, NasmEmitterScope accedingscope)
		{
			if (gpr != r)
				fw.WriteLine($"mov {gpr}, {r}");
		}

		public override void StackBackValue(Register gpr, FormatWriter fw, NasmEmitterScope accedingscope)
		{
			fw.WriteLine($"mov {r}, {gpr}");
		}

		public override bool Assignable
		{
			get
			{
				return false;
			}
		}
	}
}
