using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TigerCs.CompilationServices;
using TigerCs.Emitters.NASM;

namespace Surubi
{
	class Program
	{
		static void Main(string[] args)
		{
			var r = new ErrorReport();

			NasmEmitter e = new NasmEmitter();
			e.OutputFile = "out.asm";

			NasmType _int;
			e.TryBindSTDType("int", out _int);
			NasmType _string;
			e.TryBindSTDType("string", out _string);
			NasmFunction print;
			e.TryBindSTDFunction("prints", out print);

			e.InitializeCodeGeneration(r);

			e.EntryPoint(true, true);

			var a = e.BindVar(_int, "a");
			var _0 = e.AddConstant(0);
			e.InstrAssing(a, _0);

			var hello = e.AddConstant("hello world\n");
			var s = e.BindVar(_string, "s");
			e.InstrAssing(s, hello);
			e.Call(print, new[] { s });


			e.Ret(a);
			e.End();
		}
	}
}
