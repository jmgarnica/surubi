using TigerCs.CompilationServices;
using TigerCs.Generation.AST.Expresions;
using TigerCs.Emitters.NASM;
using System.Collections.Generic;
using TigerCs.Emitters;

namespace Surubi
{


	class Program
	{
		static void Main()
		{
			var r = new ErrorReport();
			NasmEmitter e = new NasmEmitter {OutputFile = "ex.asm"};
			DefaultSemanticChecker dsc = new DefaultSemanticChecker();

			#region [NASM Generation]

			//NasmType _int;
			//e.TryBindSTDType("int", out _int);
			//NasmType _string;
			//e.TryBindSTDType("string", out _string);
			//NasmFunction print;
			//e.TryBindSTDFunction("prints", out print);
			//NasmFunction printi;
			//e.TryBindSTDFunction("printi", out printi);
			//NasmHolder nill;
			//e.TryBindSTDConst("nill", out nill);


			//e.InitializeCodeGeneration(r);

			//e.EntryPoint(true, true);
			//var _0 = e.AddConstant(0);
			//var lend = e.AddConstant("\n");

			//#region [array]
			////var _4 = e.AddConstant(4);
			////var lf = e.AddConstant("\n");

			////var t = e.BindArrayType("intarray", _int);
			////var array = e.BindVar(t, name: "array_int");
			//////e.Call(t.Allocator, new[] { _4, _0 }, array);

			////for (int i = 0; i < 5; i++)
			////{
			////	var item = e.StaticMemberAcces(t, array, i);
			////	e.InstrAssing(item, e.AddConstant(i));
			////	e.Call(printi, new[] { item });
			////	e.Call(print, new[] { lf });
			////}
			//#endregion

			//#region [var test]
			////var a = e.BindVar(_string, e.AddConstant("hola"), "a");

			////var f = e.BindFunction("f", _int, new[] { new Tuple<string, NasmType>("x", _string) });
			////var px = e.GetParam(0);
			////var _sp = e.AddConstant(" ");
			////var _nl = e.AddConstant("\n");
			////e.Call(print, new[] { a });
			////e.Call(print, new[] { _sp });
			////e.Call(print, new[] { px });
			////e.Call(print, new[] { _nl });
			////e.Ret();
			////e.LeaveScope();

			////e.EnterNestedScope(namehint: "nested 1");
			////var hh = e.AddConstant("mundo");
			////var x = e.BindVar(_string, name: "x");
			////e.InstrAssing(x, hh);
			////e.Call(f, new[] { x });
			////e.Call(f, new[] { a });
			////e.LeaveScope();

			////e.Call(printi, new[] { _0 });
			////e.Call(printi, new[] { _0 });


			////var _2 = e.AddConstant(2);
			////var res = e.BindVar(_int, e.AddConstant('t'), "res");
			////e.InstrAssing(e.StaticMemberAcces(_string, a, 2), res);
			////e.InstrAssing(e.StaticMemberAcces(_string, a, 0), e.AddConstant('j'));
			////e.Call(print, new[] { a });

			////e.Call(_string.DynamicMemberReadAccess, new[] { a, _2 }, res);
			////e.Call(printi, new[] { res });
			//#endregion

			//#region [idiv]

			////var s = e.AddConstant(-6);
			////var d = e.BindVar();
			////e.InstrAssing(d, s);

			////var q = e.AddConstant(-4);
			////e.InstrDiv(d, d, q);

			////e.Call(printi, new[] { d });

			//#endregion

			//#region [goto]

			////var a = e.BindVar(_string, e.AddConstant("unchanged value"));
			////var label = e.ReserveInstructionLabel("jump point");

			////e.EnterNestedScope();

			////var c = e.BindVar(_int, e.AddConstant(9));

			////e.Call(printi, new[] { c });
			////e.Call(print, new[] { lend });
			////e.Call(print, new[] { a });
			////e.Call(print, new[] { lend });
			////e.Goto(label);
			////e.InstrAssing(a, e.AddConstant("changed value"));

			////e.LeaveScope();

			////e.ApplyReservedLabel(label);
			////e.Call(print, new[] { a });
			////e.Call(print, new[] { lend });

			//#endregion

			//e.Ret(_0);
			//e.LeaveScope();
			//e.End();
			#endregion

			#region AST

			var tg = new TigerGenerator<NasmType, NasmFunction, NasmHolder>(dsc, e);
			var m = new StringConstant { Lex = "Hello World" };

			tg.Compile(m);

			#endregion
		}
	}
}
