using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace CMPTest.Emission
{
	public abstract class EmissionTester<T,F,H>
		where T : class, IType<T,F>
		where F : class, IFunction<T,F>
		where H : class, IHolder
	{
		[Flags]
		protected enum STDBind
		{
			Null = 0,
			Int = 1,
			String = 2,
			Prints = 4,
			Printi = 8,
			Nil = 16
		}

		protected static Random rand = new Random();

		protected ErrorReport r { get; private set; }
		protected IByteCodeMachine<T,F,H> e;

		protected T _int { get; private set; }
		protected T _string { get; private set; }

		protected F print { get; private set; }
		protected F printi { get; private set; }

		protected H nil { get; private set; }

		protected H _0 { get; private set; }

		protected H lend { get; private set; }

		protected abstract IByteCodeMachine<T, F, H> InitBCM(string testname);

		protected abstract string Run(string[] args, string testdata, out int exitcode);

		protected virtual void Clear(string testname)
		{}

		protected string WarpRun(string[] args, string testdata, out int exitcode)
			{
			string en = Run(args, testdata, out exitcode);
			Console.WriteLine("<Test stdout>");
			Console.WriteLine(en);
			Console.WriteLine("</Test stdout>");
			Console.WriteLine($"Test exited with code {exitcode}");
			Assert.AreEqual(0, r.Count());
			return en;
		}

		protected void Init(string testname, STDBind s = STDBind.Null)
		{
			r = new ErrorReport();
			e = InitBCM(testname);
			T dum = null;

			if ((s & STDBind.Int) != 0)
				Assert.IsFalse(!e.TryBindSTDType("int", out dum));

			_int = dum;

			if ((s & STDBind.String) != 0)
				Assert.IsFalse(!e.TryBindSTDType("string", out dum));

			_string = dum;

			F fdum = null;

			if ((s & STDBind.Prints) != 0)
				Assert.IsFalse(!e.TryBindSTDFunction("prints", out fdum));

			print = fdum;

			if ((s & STDBind.Printi) != 0)
				Assert.IsFalse(!e.TryBindSTDFunction("printi", out fdum));

			printi = fdum;

			H hdum = null;

			if ((s & STDBind.Nil) != 0)
				Assert.IsFalse(!e.TryBindSTDConst("nil", out hdum));

			nil = hdum;

			e.InitializeCodeGeneration(r);
			e.EntryPoint(true, true);

			_0 = e.AddConstant(0);
			lend = e.AddConstant("\n");

			Assert.AreNotEqual(null, _0);
			Assert.AreNotEqual(null, lend);
		}

		protected void End(H ret)
		{
			e.Ret(ret);
			e.LeaveScope();
			e.End();
			Assert.AreEqual(0,r.Count());
		}

		[TestMethod]
		public void Var()
		{
			const string testname = "var";
			Init(testname, STDBind.Int | STDBind.String | STDBind.Printi | STDBind.Prints);


			var a = e.BindVar(_string, e.AddConstant("hola"), "a", HolderOptions.Trapped);

			var f = e.BindFunction("f", _int, new[] { new Tuple<string, T>("x", _string) });
			var px = e.GetParam(0);
			var _sp = e.AddConstant(" ");
			e.Call(print, new[] { a });		//outer var a: hola
			e.Call(print, new[] { _sp });	//local var sp: " "
			e.Call(print, new[] { px });	//param[0]
			e.Call(print, new[] { lend });	//outer var lend: "\n"
			e.Ret();
			e.LeaveScope();

			e.EnterNestedScope(namehint: "nested 1");
			var hh = e.AddConstant("mundo");
			var x = e.BindVar(_string, name: "x");
			e.InstrAssing(x, hh);
			e.Call(f, new[] { x });		//call f[x]: "hola mundo\n"
			e.Call(f, new[] { a });		//call f[a]: "hola hola\n"
			e.LeaveScope();

			e.Call(printi, new[] { _0 });	//_0 lend: "0\n"
			e.Call(print, new[] { lend });


			var _2 = e.AddConstant(2);
			var res = e.BindVar(_int, e.AddConstant('t'), "res");
			e.InstrAssing(e.StaticMemberAcces(_string, a, 2), res);					//a[2] = t => a = "hota"
			e.InstrAssing(e.StaticMemberAcces(_string, a, 0), e.AddConstant('j'));	//a[0] = j => a = "jota"
			e.Call(print, new[] { a });		//a lend: "jota\n"

			e.Call(print, new[] { lend });

			e.Call(_string.DynamicMemberReadAccess, new[] { a, _2 }, res);
			e.Call(printi, new[] { res });//(int)a[2]

			End(_0);

			string expected = "hola mundo" + Environment.NewLine;
			expected += "hola hola" + Environment.NewLine;
			expected += "0" + Environment.NewLine;
			expected += "jota" + Environment.NewLine;
			expected += ((int)("jota"[2])).ToString();

			int dum;
			string result = WarpRun(null, "", out dum);
			Assert.AreEqual(0, dum);
			Assert.AreEqual(expected, result);

			Clear(testname);
		}

		[TestMethod]
		public void Params()
		{
			const string testname = "params";
			Init(testname, STDBind.Int | STDBind.String | STDBind.Printi | STDBind.Prints);

			var argc = e.GetParam(0);
			e.Call(printi, new[] { argc });

			var args = e.GetParam(1);
			var strarr = e.BindArrayType("string[]", _string);

			e.Call(print, new[] { e.StaticMemberAcces(strarr, args, 0) });

			End(_0);

			int dum;
			string result = WarpRun(null, "", out dum);
			Assert.AreEqual(0, dum);
			Assert.AreEqual("1" + testname + ".exe", result);

			Clear(testname);
		}

		[TestMethod]
		public void IDiv()
		{
			const string testname = "idiv";
			Init(testname, STDBind.Int | STDBind.Printi | STDBind.String | STDBind.Prints );

			var _stringarr = e.BindArrayType("string[]", _string);

			var argsc = e.GetParam(0);
			var args = e.GetParam(1);

			var correct = e.ReserveInstructionLabel("correct");
			var res = e.InstrSub_TempBound(argsc, e.AddConstant(3));
            e.GotoIfZero(correct, res);

			e.EmitError(4, "incorrect number of arguments");

			e.ApplyReservedLabel(correct);

			var n1 = e.StaticMemberAcces(_stringarr, args, 1);
			e.InstrSize(n1,n1);
			e.Call(printi, new []{n1});
			e.Call(print, new[] { lend });

			var n2 = e.StaticMemberAcces(_stringarr, args, 2);
			e.InstrSize(n2, n2);
			e.Call(printi, new[] { n2 });
			e.Call(print, new[] { lend });

			var n_n1 = e.InstrSub_TempBound(e.AddConstant(0), n1);
			var n_n2 = e.InstrSub_TempBound(e.AddConstant(0), n2);

			#region div

			e.InstrDiv(res, n1, n1);
			e.Call(printi, new[] { res });
			e.Call(print, new[] { lend });

			e.InstrDiv(res, n1, n_n1);
			e.Call(printi, new[] { res });
			e.Call(print, new[] { lend });

			e.InstrDiv(res, n_n1, n1);
			e.Call(printi, new[] { res });
			e.Call(print, new[] { lend });

			e.InstrDiv(res, n_n1, n_n1);
			e.Call(printi, new[] { res });
			e.Call(print, new[] { lend });


			e.InstrDiv(res, n2, n2);
			e.Call(printi, new[] { res });
			e.Call(print, new[] { lend });

			e.InstrDiv(res, n2, n_n2);
			e.Call(printi, new[] { res });
			e.Call(print, new[] { lend });

			e.InstrDiv(res, n_n2, n2);
			e.Call(printi, new[] { res });
			e.Call(print, new[] { lend });

			e.InstrDiv(res, n_n2, n_n2);
			e.Call(printi, new[] { res });
			e.Call(print, new[] { lend });



			e.InstrDiv(res, n2, n1);
			e.Call(printi, new[] { res });
			e.Call(print, new[] { lend });

			e.InstrDiv(res, n2, n_n1);
			e.Call(printi, new[] { res });
			e.Call(print, new[] { lend });

			e.InstrDiv(res, n_n2, n1);
			e.Call(printi, new[] { res });
			e.Call(print, new[] { lend });

			e.InstrDiv(res, n_n2, n_n1);
			e.Call(printi, new[] { res });
			e.Call(print, new[] { lend });


			e.InstrDiv(res, n1, n2);
			e.Call(printi, new[] { res });
			e.Call(print, new[] { lend });

			e.InstrDiv(res, n1, n_n2);
			e.Call(printi, new[] { res });
			e.Call(print, new[] { lend });

			e.InstrDiv(res, n_n1, n2);
			e.Call(printi, new[] { res });
			e.Call(print, new[] { lend });

			e.InstrDiv(res, n_n1, n_n2);
			e.Call(printi, new[] { res });
			e.Call(print, new[] { lend });

			#endregion

			End(_0);

			int a = rand.Next(100) + 1;
			int b = rand.Next(100) + 1;

			string expected = $"{a}{Environment.NewLine}{b}{Environment.NewLine}";
			expected += $"{a / a}{Environment.NewLine}";
			expected += $"{a / -a}{Environment.NewLine}";
			expected += $"{-a / a}{Environment.NewLine}";
			expected += $"{-a / -a}{Environment.NewLine}";

			expected += $"{b / b}{Environment.NewLine}";
			expected += $"{b / -b}{Environment.NewLine}";
			expected += $"{-b / b}{Environment.NewLine}";
			expected += $"{-b / -b}{Environment.NewLine}";

			expected += $"{b / a}{Environment.NewLine}";
			expected += $"{b / -a}{Environment.NewLine}";
			expected += $"{-b / a}{Environment.NewLine}";
			expected += $"{-b / -a}{Environment.NewLine}";

			expected += $"{a / b}{Environment.NewLine}";
			expected += $"{a / -b}{Environment.NewLine}";
			expected += $"{-a / b}{Environment.NewLine}";
			expected += $"{-a / -b}{Environment.NewLine}";

			int dum;
			string result = WarpRun(new []{new string('1', a), new string('1', b) }, "", out dum);
			Assert.AreEqual(0, dum);
			Assert.AreEqual(expected, result);

			//Clear(testname);
		}

		[TestMethod]
		public void Goto()
		{
			const string testname = "goto";
			Init(testname, STDBind.Int | STDBind.String | STDBind.Printi | STDBind.Prints);
			string expected = "";
			const string unchanged = "unchanged value", changed = "changed value";


			var a = e.BindVar(_string, e.AddConstant(unchanged));

			e.EnterNestedScope();

			var c = e.BindVar(_int, e.AddConstant(9));
			var label = e.ReserveInstructionLabel("jump point");

			e.Call(printi, new[] { c });
			expected += "9";

			e.Call(print, new[] { lend });
			expected += Environment.NewLine;

			e.Call(print, new[] { a });
			expected += unchanged;

			e.Call(print, new[] { lend });
			expected += Environment.NewLine;

			e.Goto(label);
			e.InstrAssing(a, e.AddConstant(changed));

			e.ApplyReservedLabel(label);
			e.Call(print, new[] { a });
			expected += unchanged;

			e.Call(print, new[] { lend });
			expected += Environment.NewLine;

			e.LeaveScope();

			End(_0);

			int dum;
			string result = WarpRun(null, "", out dum);
			Assert.AreEqual(0, dum);
			Assert.AreEqual(expected, result);

			Clear(testname);
		}

		[TestMethod]
		public void Array()
		{
			const string testname = "array";
			const int size = 10;
			Init(testname, STDBind.Int | STDBind.Printi | STDBind.Prints);

			var _size = e.AddConstant(size);
			var lf = e.AddConstant("\n");

			var t = e.BindArrayType("int[]", _int);
			var array = e.BindVar(t, name: "array_int");
			e.Call(t.Allocator, new[] { _size, _0 }, array);

			for (int i = 0; i < size; i++)
			{
				var item = e.StaticMemberAcces(t, array, i);
				e.InstrAssing(item, e.AddConstant(i));
			}

			var sz = e.BindVar(_int, e.AddConstant(0));
			e.InstrSize(array, sz);
			var el = e.BindVar(_int);
			var _i = e.BindVar(_int, e.AddConstant(0));

			//foreach

			var end = e.ReserveInstructionLabel("END");
			var loop = e.SetLabelToNextInstruction("loop");
			var cmp = e.InstrSub_TempBound(sz, _i);
			e.GotoIfZero(end, cmp);

			e.Call(t.DynamicMemberReadAccess, new []{array, _i}, el);
			e.Call(printi, new[] { el });
			e.Call(print, new[] { lf });

			e.InstrAdd(_i, _i, e.AddConstant(1));
			e.Goto(loop);
			e.ApplyReservedLabel(end);

			End(_0);

			int dum;
			string result = WarpRun(null, "", out dum);
			Assert.AreEqual(0, dum);

			string expected = "";
			for (int i = 0; i < size; i++)
				expected += i + Environment.NewLine;

			Assert.AreEqual(expected, result);

			Clear(testname);
		}

		[TestMethod]
		public void Closure()
		{
			const string testname = "closures";
			Init(testname, STDBind.Int | STDBind.String | STDBind.Printi | STDBind.Prints);
			const int inc = 1;
			const int start = 0;
			const int max = 10;

			var ff = e.BindVar();
			var gg = e.BindVar();

			e.EnterNestedScope(namehint: "closure one");
			var c = e.BindVar(_int, e.AddConstant(start), "counter", HolderOptions.Trapped);

			e.Call(print, new[] { e.AddConstant("[trapped]c = ") });
			e.Call(printi, new[] { c });
			e.Call(print, new[] { lend });

			var f = e.BindFunction("print_c", _int, new Tuple<string, T>[0], FunctionOptions.Delegate);
			e.Call(print, new[] { e.AddConstant(" = ") });
			e.Call(printi, new[] { c });
			e.Call(print, new[] { lend });
			e.Ret();
			e.LeaveScope();
			e.MekeDelegate(ff, f);

			var g = e.BindFunction("increment_c", _int, new[] { new Tuple<string, T>("inc", _int) }, FunctionOptions.Delegate);
			var _inc = e.GetParam(0);
			e.Call(printi, new[] { c });
			e.Call(print, new[] { e.AddConstant(" + ") });
			e.Call(printi, new[] { _inc });
			e.InstrAdd(c, c, _inc);
			e.Ret();
			e.LeaveScope();
			e.MekeDelegate(gg, g);

			e.LeaveScope();

			e.EnterNestedScope(namehint: "nested 1");

			var i = e.BindVar(_int, e.AddConstant(0), "i");
			var loop = e.SetLabelToNextInstruction("loop");
			var end = e.ReserveInstructionLabel("END");

			var tmp = e.InstrSub_TempBound(i, e.AddConstant(max));
			e.GotoIfNotNegative(end, tmp);

			e.DelegateCall(gg, new[] { i });
			e.DelegateCall(ff, new H[0]);

			e.InstrAdd(i, i, e.AddConstant(inc));

			e.Goto(loop);
			e.ApplyReservedLabel(end);


			e.LeaveScope();

			End(_0);

			int _c = start;
			string expected = "[trapped]c = " + _c + Environment.NewLine;
			for (int j = 0; j < max; j+= inc)
				expected += _c + " + " + j + " = " + (_c += j) + Environment.NewLine;


			int dum;
			string result = WarpRun(null, "", out dum);
			Assert.AreEqual(0, dum);
			Assert.AreEqual(expected, result);

			Clear(testname);
		}
	}
}
//TODO: add atomic and simpler tests
//TODO: negative tests