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
			e.OutputFile = "ex.asm";

			NasmType _int;
			e.TryBindSTDType("int", out _int);
			NasmType _string;
			e.TryBindSTDType("string", out _string);
			NasmFunction print;
			e.TryBindSTDFunction("prints", out print);
			NasmFunction printi;
			e.TryBindSTDFunction("printi", out printi);


			e.InitializeCodeGeneration(r);

			e.EntryPoint(true, true);
			var _0 = e.AddConstant(0);

			var a = e.BindVar(_string, e.AddConstant("hola"), "a");

			//var f = e.BindFunction("f", _int, new[] { new Tuple<string, NasmType>("x", _string) });
			//var px = e.GetParam(0);
			//var _sp = e.AddConstant(" ");
			//var _nl = e.AddConstant("\n");
			//e.Call(print, new[] { a });
			//e.Call(print, new[] { _sp });
			//e.Call(print, new[] { px });
			//e.Call(print, new[] { _nl });
			//e.Ret();
			//e.LeaveScope();

			//e.EnterNestedScope(namehint: "nested 1");
			//var hh = e.AddConstant("mundo");
			//var x = e.BindVar(_string, "x");
			//e.InstrAssing(x, hh);
			//e.Call(f, new[] { x });
			//e.Call(f, new[] { a });
			//e.LeaveScope();

			//e.Call(printi, new[] { _0 });
			//e.Call(printi, new[] { _0 });


			var _2 = e.AddConstant(2);
			var res = e.BindVar(_int, e.AddConstant('t'), "res");
			e.InstrAssing(e.StaticMemberAcces(_string, a, 2), res);
			e.InstrAssing(e.StaticMemberAcces(_string, a, 0), e.AddConstant('j'));
			e.Call(print, new[] { a });

			e.Call(_string.DynamicMemberReadAccess, new[] { a, _2 }, res);
			e.Call(printi, new[] { res });

			e.Ret(_0);
			e.LeaveScope();
			e.End();
			
		}
	}
}
