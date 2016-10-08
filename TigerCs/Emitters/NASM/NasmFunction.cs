using System;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Emitters.NASM
{
	public class NasmFunction : NasmMember, IFunction<NasmType, NasmFunction>
	{
		public static NasmFunction Malloc, Free;

		NasmEmitter bound;

		public NasmFunction(NasmEmitterScope dscope, int sindex, NasmEmitter bound)
			: base(dscope, sindex)
		{
			this.bound = bound;
		}

		public bool CFunction { get; protected set; }

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

		public void Call(FormatWriter fw, Register? result, NasmEmitterScope accedingscope, params NasmMember[] args)
		{
			var lockreglist = accedingscope.Lock.Locked();
			fw.WriteLine(string.Format(";before calling {0}", Name));

			bool pop = false;
			foreach (var r in lockreglist)
			{
				if (!r.GeneralPurposeRegister() || (result != null && result == r)) continue;
				fw.WriteLine("push " + r);
				pop = true;
				accedingscope.Lock.Release(r);
			}

			fw.WriteLine(string.Format(";getting {0}", Name));
			accedingscope.Lock.Lock(Register.EAX);
			PutValueInRegister(Register.EAX, fw, accedingscope);

			if (!CFunction)
			{
				fw.WriteLine(string.Format(";{0}.EBP", Name));
				fw.WriteLine("mov EDX, [EAX + 4]");
				fw.WriteLine("push EDX");
			}

			fw.WriteLine(string.Format(";{0}.Params", Name));
			accedingscope.Lock.Lock(Register.EDX);
			foreach (var arg in args)
			{
				arg.PutValueInRegister(Register.EDX, fw, accedingscope);
				fw.WriteLine("push " + Register.EDX);				
			}
			fw.WriteLine("");
			accedingscope.Lock.Release(Register.EDX);

			if (!CFunction)
			{
				fw.WriteLine(string.Format(";{0}.F*", Name));
				fw.WriteLine("mov EAX, [EAX]");
			}

			fw.WriteLine("call EAX");
			fw.WriteLine(string.Format("add ESP, {0}", (CFunction ? args.Length : args.Length + 1) * 4));

			bound.CatchAndRethrow();
			bound.SetLabel();
			if (result != null && result != Register.EAX)
			{
				fw.WriteLine(string.Format("mov {0}, EAX", result.Value));
			}
			else if (!pop) fw.WriteLine("nop");
			if (result != Register.EAX) accedingscope.Lock.Release(Register.EAX);

			foreach (var r in lockreglist)
			{
				if (!r.GeneralPurposeRegister() || (result != null && result == r)) continue;
				fw.WriteLine("pop " + r);
				accedingscope.Lock.Lock(r);
			}
		}

		public void DealocateFunction(FormatWriter fw, NasmEmitterScope accedingscope) => Free.Call(fw, null, accedingscope, this);

		public static void AlocateFunction(FormatWriter fw, Register target, NasmEmitterScope accedingscope, NasmEmitter bound)
		{
			var sp = bound.AddConstant(8);
			Malloc.Call(fw, target, accedingscope, sp);
		}
	}

	public class NasmCFunction : NasmFunction
	{
		string label;

		public NasmCFunction(string label, bool exf, NasmEmitter bound)
			:base(null, -1, bound)
		{
			this.label = label;
			if(exf) bound.AddExtern(label);
			CFunction = true;
		}

		public override void PutValueInRegister(Register gpr, FormatWriter fw, NasmEmitterScope accedingscope)
		{
			fw.WriteLine(string.Format("mov {0}, {1}", gpr, label));
		}

		public override void StackBackValue(Register gpr, FormatWriter fw, NasmEmitterScope accedingscope)
		{
			throw new InvalidOperationException("CFunction are inmutable");
		}
	}
}
