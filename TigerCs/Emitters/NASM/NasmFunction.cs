using System;
using System.Collections.Generic;
using TigerCs.Generation.ByteCode;
using System.Linq;

namespace TigerCs.Emitters.NASM
{
	public class NasmFunction : NasmMember, IFunction<NasmType, NasmFunction>
	{
		public static NasmFunction Malloc, Free;

		protected NasmEmitter bound;

		public NasmFunction(NasmEmitterScope dscope, int sindex, NasmEmitter bound, string name = "")
			: base(dscope, sindex)
		{
			this.bound = bound;
			Name = name;
		}

		public bool CFunction { get; set; }
		public bool ErrorCheck { get; protected set; } = true;

		public string Name { get; private set; }
		public int ParamsCount { get; set; }
		public bool Bounded
		{
			get; set;
		}

		public NasmType Return
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public virtual void Call(FormatWriter fw, Register? result, NasmEmitterScope accedingscope, params NasmMember[] args)
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

			fw.WriteLine(string.Format(";calling {0}", Name));
			fw.WriteLine("call EAX");
			fw.WriteLine(string.Format("add ESP, {0}", (CFunction ? args.Length : args.Length + 1) * 4));
			if (ErrorCheck)
			{
				fw.WriteLine(string.Format(";error catching", Name));
				NasmEmitter.CatchAndRethrow(fw, accedingscope, bound);				
			}
			if (result != null && result != Register.EAX)
			{
				fw.WriteLine(string.Format("mov {0}, EAX", result.Value));
			}
			else if (bound.SetLabel() && !pop) fw.WriteLine("nop");
			if (result != Register.EAX) accedingscope.Lock.Release(Register.EAX);

			foreach (var r in lockreglist)
			{
				if (!r.GeneralPurposeRegister() || (result != null && result == r)) continue;
				fw.WriteLine("pop " + r);
				accedingscope.Lock.Lock(r);
			}
		}

		public void DealocateFunction(FormatWriter fw, NasmEmitterScope accedingscope) => Free.Call(fw, null, accedingscope, this);

		public static void AlocateFunction(FormatWriter fw, Register target, NasmEmitterScope accedingscope, NasmEmitter bound, Guid beforeenterlabel)
		{
			var sp = bound.AddConstant(8);
			Malloc.Call(fw, target, accedingscope, sp);
			fw.WriteLine(string.Format("mov [{0}], dword _{1}", target, beforeenterlabel.ToString("N")));
			fw.WriteLine(string.Format("mov [{0} + 4], EBP", target));
		}
	}

	public class NasmCFunction : NasmFunction
	{
		string label;

		public NasmCFunction(string label, bool exf, NasmEmitter bound, bool errorcheck = false, string name = "")
			:base(null, -1, bound, name)
		{
			this.label = label;
			if(exf) bound.AddExtern(label);
			ErrorCheck = errorcheck;
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

	/// <summary>
	/// A scope independet, register pareametr passing macro
	/// </summary>
	public class NasmMacroFunction : NasmFunction
	{
		/// <summary>
		/// Return value always in args[0] or EAX when args.Length = 0
		/// </summary>		
		public delegate void MacroCall(FormatWriter fw, NasmEmitter bound, NasmEmitterScope acceding, Register[] args);

		/// <summary>
		/// If not null must contains one register per expected argumet and the argumets are guaranteed to follow this order 
		/// </summary>
		public Register[] Requested { get; set; }

		public readonly MacroCall CallPoint;
		public NasmMacroFunction(MacroCall call, NasmEmitter bound, string macroname = "")
			:base(null, -1, bound, macroname)
		{
			CallPoint = call;
		}

		public override void PutValueInRegister(Register gpr, FormatWriter fw, NasmEmitterScope accedingscope)
		{
			throw new InvalidOperationException("Macros have no pointer form");
		}

		public override void StackBackValue(Register gpr, FormatWriter fw, NasmEmitterScope accedingscope)
		{
			throw new InvalidOperationException("Macros have no pointer form");
		}

		public override void Call(FormatWriter fw, Register? result, NasmEmitterScope accedingscope, params NasmMember[] args)
		{
			if (args.Length > 4) throw new ArgumentException("Macros can't have more than 4 parameters");
			fw.WriteLine(string.Format(";before calling {0}", Name));
			var pops = new List<Register>(4);
			var param = new List<Register>(4);
			if (Requested == null)
			{
				for (int i = 0; i < args.Length; i++)
				{
					var d = (Register)(1 << i);
					var reg = accedingscope.Lock.LockGPR(d);
					if (reg == null)
					{
						pops.Add(d);
						fw.WriteLine(string.Format("push {0}", d));
						reg = d;
					}
					args[i].PutValueInRegister(reg.Value, fw, accedingscope);
					param.Add(reg.Value);
				}
			}
			else
			{
				if (Requested.Length != args.Length)
					throw new ArgumentException("unexpected arguments number");
				param.AddRange(Requested);
				for (int i = 0; i < args.Length; i++)
				{
					if (accedingscope.Lock.Locked(Requested[i]))
					{
						pops.Add(Requested[i]);
						fw.WriteLine(string.Format("push {0}", Requested[i]));
					}
					args[i].PutValueInRegister(Requested[i], fw, accedingscope);
				}
					
			}
			if (result != null && param.Count == 0 && accedingscope.Lock.Locked(Register.EAX))
			{
				pops.Add(Register.EAX);
				fw.WriteLine(string.Format("push {0}", Register.EAX));
			}
			fw.WriteLine(string.Format(";calling Macro {0}", Name));
			CallPoint(fw, bound, accedingscope, param.ToArray());

			if (result != null)
			{
				if (param.Count > 0)
				{
					if (result != param[0])
						fw.WriteLine(string.Format("mov {0}, {1}", result.Value, param[0]));
				}
				else if(result != Register.EAX)
					fw.WriteLine(string.Format("mov {0}, EAX", result.Value));
			}

			for (int i = pops.Count - 1; i >= 0; i--)
				fw.WriteLine(string.Format("pop {0}", pops[i]));

			foreach (var r in param.Except(pops))
			{
				accedingscope.Lock.Release(r);
			}
		}
	}
}
