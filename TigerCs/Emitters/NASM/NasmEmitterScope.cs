using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TigerCs.Emitters.NASM
{
	public class NasmEmitterScope : EmitterScope<NasmEmitterScope>
	{
		public RegisterLock Lock { get; set; }
		public int VarsCount { get; set; }
		public readonly int ArgumentsCount;
		public readonly NasmScopeType ScopeType;
		public readonly NasmFunction DeclaringFunction;

		public List<NasmFunction> FuncTypePos { get; private set; }
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

		public void WriteEnteringCode(FormatWriter fw)
		{
			fw.WriteLine("push " + Register.EBP);
			if (ScopeType == NasmScopeType.TigerFunction)
			{
				fw.WriteLine("mov EBX, ESP");
				fw.WriteLine(string.Format("add EBX, {0}", 4 * (ArgumentsCount + 2)));
				fw.WriteLine("mov EBX, [EBX]");
				fw.WriteLine("push EBX");
			}
			fw.WriteLine("mov EBP, ESP");
			fw.WriteLine(string.Format("sub ESP, {1}{0}{2}", fw.IndexOfFormat, '{', '}'), (Func<string>)(() => (VarsCount*4).ToString()));
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
					m.DealocateFunction(f, this);

				if (ScopeType == NasmScopeType.TigerFunction || ScopeType == NasmScopeType.CFunction)
				{
					if (ret != null) ret.PutValueInRegister(Register.EAX, f, this);

					if (error)
						f.WriteLine(string.Format("mov ECX, {0}", NasmEmitter.ErrorCode));
					else f.WriteLine("xor ECX, ECX");
				}

				f.WriteLine(string.Format("add ESP, {0}", 4 * (ScopeType == NasmScopeType.TigerFunction ? VarsCount + 1 : VarsCount)));
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
