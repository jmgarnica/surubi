using System;
using System.Collections.Generic;
using TigerCs.Generation.ByteCode;
using System.Linq;

namespace TigerCs.Emitters.NASM
{
	using CompilationServices;

	public class NasmFunction : NasmMember, IFunction<NasmType, NasmFunction>
	{
		public static NasmFunction Malloc, Free;

		public NasmFunction(NasmEmitterScope dscope, int sindex, NasmEmitter bound, string name = "")
			: base(bound, dscope, sindex)
		{
			Name = name;
		}

		public bool CFunction { get; set; }
		public bool ErrorCheck { get; protected set; } = true;
		public bool KeepOutScope { get; set; }

		public string Name { get; }
		public int ParamsCount { get; set; }
		public bool Bounded
		{
			get; set;
		}

		public NasmType Return { get; set; }

		public virtual void Call(FormatWriter fw, Register? result, NasmEmitterScope accedingscope, params NasmMember[] args)
		{
			var lockreglist = accedingscope.Lock.Locked();
			fw.WriteLine($";before calling {Name}");

			foreach (var r in lockreglist)
			{
				if (!r.GeneralPurposeRegister() || (result != null && result == r)) continue;
				fw.WriteLine("push " + r);
				accedingscope.Lock.Release(r);
			}
			lockreglist.Reverse();

			fw.WriteLine($";getting {Name}");
			accedingscope.Lock.Lock(Register.EAX);
			PutValueInRegister(Register.EAX, fw, accedingscope);

			if (!CFunction)
			{
				fw.WriteLine($";{Name}.EBP");
				fw.WriteLine("mov EDX, [EAX + 4]");
				fw.WriteLine("push EDX");
			}

			fw.WriteLine($";{Name}.Params");
			accedingscope.Lock.Lock(Register.EDX);
			foreach (var arg in args.Reverse())
			{
				arg.PutValueInRegister(Register.EDX, fw, accedingscope);
				fw.WriteLine("push " + Register.EDX);
			}
			fw.WriteLine("");
			accedingscope.Lock.Release(Register.EDX);

			if (!CFunction)
			{
				fw.WriteLine($";{Name}.F*");
				fw.WriteLine("mov EAX, [EAX]");
			}

			fw.WriteLine($";calling {Name}");
			fw.WriteLine("call EAX");
			fw.WriteLine($"add ESP, {(CFunction? args.Length : args.Length + 1) * 4}");
			if (ErrorCheck)
			{
				fw.WriteLine($";error catching {Name}");
				NasmEmitter.CatchAndRethrow(fw, accedingscope, bound, lockreglist.Count);
			}
			if (result != null && result != Register.EAX)
				fw.WriteLine($"mov {result.Value}, EAX");
			accedingscope.Lock.Release(Register.EAX);

			foreach (var r in lockreglist)
			{
				if (!r.GeneralPurposeRegister() || (result != null && result == r)) continue;
				fw.WriteLine("pop " + r);
				accedingscope.Lock.Lock(r);
			}
		}

		public void DealocateFunction(FormatWriter fw, NasmEmitterScope accedingscope) => Free.Call(fw, null, accedingscope, this);

		public static void AlocateFunction(FormatWriter fw, Register target, NasmEmitterScope accedingscope, NasmEmitter bound, Guid beforeenterlabel, bool closure)
		{
			var sp = bound.AddConstant(8);
			Malloc.Call(fw, target, accedingscope, sp);
			fw.WriteLine($"mov [{target}], dword _{beforeenterlabel:N}");

			var reg = accedingscope.Lock.LockGPR(Register.EBX);
			bool stackback = reg == null;
			if (reg == null)
			{
				reg = target != Register.EBX? Register.EBX : Register.EDX;
                fw.WriteLine($"push {reg}");
			}

			if (closure)
			{
				fw.WriteLine($"mov {reg}, [EBP - 4]");
				fw.WriteLine($"add {reg}, 8");
				fw.WriteLine($"mov [{target} + 4], {reg}");
			}
			else
			{
				fw.WriteLine($"mov [{target} + 4], EBP");
			}

			if (stackback)
				fw.WriteLine($"pop {reg}");
			else accedingscope.Lock.Release(reg.Value);
		}
	}

	public class NasmCFunction : NasmFunction
	{
		readonly string label;

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
			fw.WriteLine($"mov {gpr}, {label}");
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
			bound.Report.Add(new StaticError(bound.SourceLine, bound.SourceColumn, "Macros have no pointer form",
			                                      ErrorLevel.Internal));
		}

		public override void StackBackValue(Register gpr, FormatWriter fw, NasmEmitterScope accedingscope)
		{
			throw new InvalidOperationException("Macros have no pointer form");
		}

		public override void Call(FormatWriter fw, Register? result, NasmEmitterScope accedingscope, params NasmMember[] args)
		{
			if (args.Length > 4) throw new ArgumentException("Macros can't have more than 4 parameters");
			fw.WriteLine($";before calling {Name}");
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
						fw.WriteLine($"push {d}");
						reg = d;
					}
					args[i].PutValueInRegister(reg.Value, fw, accedingscope);
					param.Add(reg.Value);
				}
			}
			else
			{
				if (Requested.Length != args.Length)
					bound.Report.Add(new StaticError(bound.SourceLine, bound.SourceColumn,
					                                      "Calling a marco with the wrong arguments number", ErrorLevel.Internal));
				param.AddRange(Requested);
				for (int i = 0; i < args.Length; i++)
				{
					if (accedingscope.Lock.Locked(Requested[i]))
					{
						pops.Add(Requested[i]);
						fw.WriteLine($"push {Requested[i]}");
					}
					args[i].PutValueInRegister(Requested[i], fw, accedingscope);
				}

			}
			if (result != null && param.Count == 0 && accedingscope.Lock.Locked(Register.EAX))
			{
				pops.Add(Register.EAX);
				fw.WriteLine($"push {Register.EAX}");
			}
			fw.WriteLine($";calling Macro {Name}");
			CallPoint(fw, bound, accedingscope, param.ToArray());

			if (result != null)
				if (param.Count > 0)
				{
					if (result != param[0])
						fw.WriteLine($"mov {result.Value}, {param[0]}");
				}
				else if(result != Register.EAX)
					fw.WriteLine($"mov {result.Value}, EAX");

			for (int i = pops.Count - 1; i >= 0; i--)
				fw.WriteLine($"pop {pops[i]}");

			foreach (var r in param.Except(pops))
				accedingscope.Lock.Release(r);
		}
	}
}
