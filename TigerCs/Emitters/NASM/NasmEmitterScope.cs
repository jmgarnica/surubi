using System;
using System.Collections.Generic;

namespace TigerCs.Emitters.NASM
{
	public class NasmEmitterScope : EmitterScope<NasmEmitterScope>
	{
		public RegisterLock Lock { get; set; }
		public int VarsCount { get; set; } = 2;
		public int TrappedVarsCount { get; set; } = 0;
		public readonly int ArgumentsCount;
		public readonly NasmScopeType ScopeType;
		public readonly NasmFunction DeclaringFunction;

		public List<NasmFunction> FuncTypePos { get; }
		public Queue<int> ReleasedTempVars { get; set; }

		public NasmEmitterScope(NasmEmitterScope parent, Guid beforeenterlabel, Guid biginlabel, Guid endlabel, Guid afterend, NasmScopeType function = NasmScopeType.Nested, int argscount = 0, NasmFunction declaringfucn = null)
			: base(parent, beforeenterlabel, biginlabel, endlabel, afterend)
		{
			Lock = new RegisterLock();
			FuncTypePos = new List<NasmFunction>();
			ArgumentsCount = argscount;
			ScopeType = function;
			ReleasedTempVars = new Queue<int>();
			DeclaringFunction = declaringfucn;
		}

		public void WriteEnteringCode(FormatWriter fw, NasmEmitter bound)
		{
			fw.WriteLine("push EBP");
			if (ScopeType == NasmScopeType.TigerFunction)
			{
				fw.WriteLine("mov EBX, ESP");
				fw.WriteLine($"add EBX, {4 * (ArgumentsCount + 2)}");
				fw.WriteLine("mov EBX, [EBX]");
				fw.WriteLine("push EBX");
			}
			fw.WriteLine("mov EBP, ESP");
			fw.WriteLine(string.Format("sub ESP, {1}{0}{2}", fw.IndexOfFormat, '{', '}'), (Func<string>)(() => (VarsCount*4).ToString()));

			//Closure

			int indent = fw.IndentationLevel;
			var scope = bound.CurrentScope;
			fw.WriteLine(string.Format("{0}{2}{1}",'{', '}', fw.IndexOfFormat), (Func<string>)(() =>
			{
				FormatWriter f = new FormatWriter();

				var cscope = bound.CurrentScope;
				bound.CurrentScope = scope;
				NasmFunction.Malloc.Call(f, Register.EAX, this, bound.AddConstant(TrappedVarsCount * 4 + 8));
				bound.CurrentScope = cscope;

				f.WriteLine("");
				if(TrappedVarsCount != 1)
					f.WriteLine($"add EAX, {TrappedVarsCount*4 - 4}");
				f.WriteLine("mov [EBP - 4], EAX");
				f.WriteLine("mov [EAX + 4], EAX");
				f.WriteLine("add EAX, 8");
				f.WriteLine("");
				if (Parent == null || ScopeType == NasmScopeType.TigerFunction)
				{
					f.WriteLine("xor EDX, EDX");
				}
				else
				{
					f.WriteLine("mov EDX, [EBP]");
					f.WriteLine("mov EDX, [EDX - 4]");
					f.WriteLine("add EDX, 8");
				}
				f.WriteLine("mov [EAX], EDX");
				f.WriteLine("");
				return FormatWriter.Indent(f.Flush(), indent);
			}));

		}

		public void WirteCloseCode(FormatWriter fw, bool releaselocks, bool error, NasmHolder ret = null)
		{
			int indent = fw.IndentationLevel;
			var l = Lock.CloneState();
			fw.Write(string.Format("{1}{0}{2}", fw.IndexOfFormat, '{', '}'), (Func<string>)(() =>
			{
				var ll = Lock;
				Lock = l;
				FormatWriter f = new FormatWriter();
				foreach (var m in FuncTypePos)
				{
					if(!m.KeepOutScope)
						m.DealocateFunction(f, this);
				}

				if (ScopeType == NasmScopeType.TigerFunction || ScopeType == NasmScopeType.CFunction)
				{
					ret?.PutValueInRegister(Register.EAX, f, this);

					f.WriteLine(error? $"mov ECX, {NasmEmitter.ErrorCode}" : "xor ECX, ECX");
				}

				f.WriteLine($"add ESP, {4 * (ScopeType == NasmScopeType.TigerFunction? VarsCount + 1 : VarsCount)}");
				f.WriteLine("pop EBP");

				if (ScopeType == NasmScopeType.TigerFunction || ScopeType == NasmScopeType.CFunction)
					f.WriteLine("ret");
				Lock = ll;

				return FormatWriter.Indent(f.Flush(), indent);
				//return f.Flush();
			}));

			if (releaselocks) Lock.ReleaseAll();
		}
	}

	public enum NasmScopeType
	{
		Nested,
		TigerFunction,
		CFunction
	}
}
