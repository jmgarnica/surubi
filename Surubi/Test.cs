using TigerCs.CompilationServices;
using TigerCs.Emitters.NASM;
using TigerCs.Emitters;
using System;
using System.Diagnostics;
using TigerCs.Generation.AST.Expressions;

namespace Surubi
{

	class Test
	{
		static void Main(string[] args)
		{
			var r = new ErrorReport();
			NasmEmitter e = new NasmEmitter("ex.asm");
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
			//NasmHolder nil;
			//e.TryBindSTDConst("nil", out nil);


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

			#region [var test]
			//var a = e.BindVar(_string, e.AddConstant("hola"), "a", HolderOptions.Trapped);

			//var f = e.BindFunction("f", _int, new[] { new Tuple<string, NasmType>("x", _string) });
			//var px = e.GetParam(0);
			//var _sp = e.AddConstant(" ");
			//e.Call(print, new[] { a });
			//e.Call(print, new[] { _sp });
			//e.Call(print, new[] { px });
			//e.Call(print, new[] { lend });
			//e.Ret();
			//e.LeaveScope();

			//e.EnterNestedScope(namehint: "nested 1");
			//var hh = e.AddConstant("mundo");
			//var x = e.BindVar(_string, name: "x");
			//e.InstrAssing(x, hh);
			//e.Call(f, new[] { x });
			//e.Call(f, new[] { a });
			//e.LeaveScope();

			//e.Call(printi, new[] { _0 });
			//e.Call(print, new[] { lend });


			//var _2 = e.AddConstant(2);
			//var res = e.BindVar(_int, e.AddConstant('t'), "res");
			//e.InstrAssing(e.StaticMemberAcces(_string, a, 2), res);
			//e.InstrAssing(e.StaticMemberAcces(_string, a, 0), e.AddConstant('j'));
			//e.Call(print, new[] { a });

			//e.Call(print, new[] { lend });

			//e.Call(_string.DynamicMemberReadAccess, new[] { a, _2 }, res);
			//e.Call(printi, new[] { res });
			#endregion

			#region [Closure test]

			//var ff = e.BindVar();
			//var gg = e.BindVar();

			//e.EnterNestedScope(namehint: "closure one");
			//var c = e.BindVar(_int, e.AddConstant(0), "counter", HolderOptions.Trapped);

			//e.Call(print, new[] { e.AddConstant("[static]c = ") });
			//e.Call(printi, new[] { c });
			//e.Call(print, new[] { lend });

			//var f = e.BindFunction("print_c", _int, new Tuple<string, NasmType>[0], FunctionOptions.Delegate);
			//e.Call(print, new[] { e.AddConstant(" = ") });
			//e.Call(printi, new[] { c });
			//e.Call(print, new[] { lend });
			//e.Ret();
			//e.LeaveScope();
			//e.MekeDelegate(ff, f);

			//var g = e.BindFunction("increment_c", _int, new[] { new Tuple<string, NasmType>("inc", _int) }, FunctionOptions.Delegate);
			//var inc = e.GetParam(0);
			//e.Call(printi, new[] { c });
			//e.Call(print, new[] { e.AddConstant(" + ") });
			//e.Call(printi, new[] { inc });
			//e.InstrAdd(c, c, inc);
			//e.Ret();
			//e.LeaveScope();
			//e.MekeDelegate(gg, g);

			//e.LeaveScope();

			//e.EnterNestedScope(namehint: "nested 1");

			//var i = e.BindVar(_int, e.AddConstant(0), "i");
			//var loop = e.SetLabelToNextInstruction("loop");
			//var end = e.ReserveInstructionLabel("END");

			//var tmp = e.InstrRefEq_TempBound(i, e.AddConstant(10));
			//e.GotoIfNotZero(end, tmp);

			//e.DelegateCall(gg, new[] { i });
			//e.DelegateCall(ff, new NasmHolder[0]);

			//e.InstrAdd(i, i, e.AddConstant(1));

			//e.Goto(loop);
			//e.ApplyReservedLabel(end);


			//e.LeaveScope();
			#endregion

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

			TigerGenerator<NasmType, NasmFunction, NasmHolder> tg = new TigerGenerator<NasmType, NasmFunction, NasmHolder>
			{
				SemanticChecker = dsc,
				ByteCodeMachine = e
			};

			#region hello world

			var m = new BoundedFor
			{
				VarName = "i",
				From = new IntegerConstant {Lex = "0"},
				To = new IntegerConstant {Lex = "10"},
				Body = new Call
				{
					Arguments = new ExpressionList<IExpression> {new Var {Name = "i"}},
					FunctionName = "printi"
				}
			};

			#endregion

			tg.Compile(m);

			int count = tg.Report.Count();
			Console.WriteLine("Compilation " + (count == 0
													? "success"
													: $"fail with {count} error{(count > 1 ? "s" : "")}:"));

			Console.WriteLine();

			foreach (var error in tg.Report)
				Console.WriteLine(error);

			if (Debugger.IsAttached) Console.ReadKey();

			#endregion
		}
	}
}
