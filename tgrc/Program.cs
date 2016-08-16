using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TigerCs.Emitters.CSharp;
using TigerCs.Generation.ByteCode;
using TigerCs.Generation.Semantic.Scopes;

namespace tgrc
{
	class Program
	{
		static void Main(string[] args)
		{

		}

		static void TestFile()
		{
			//FileStream fs = new FileStream(@"E:\Jesus\Documentos\C.Computacion\Proyectos\Dockyard\TEST\test\test.cs", FileMode.Create, FileAccess.Write);
			//StreamWriter w = new StreamWriter(fs);

			//ITigerEmitter em = new CSharpEmitter();
			//em.InitializeCodeGeneration(w, null);
			//test_TigerEmitter(em);
			//em.End();
			//w.Close();
			
		}

		static void TestConsole()
		{
			//ITigerEmitter em = new CSharpEmitter();
			//em.InitializeCodeGeneration(Console.Out, null);
			//test_TigerEmitter(em);
			//em.End();
		}

		static void test_TigerEmitter(ITigerEmitter em)
		{
			em.SetLabelToNextInstruction("loop");
			var a = em.BoundVar(em.String.Type, "a");
			MemberInfo fnc;
			em.CurrentScope.Reachable("getchar", out fnc);
			em.BlankLine();

			em.Call((FunctionInfo)fnc, 0, a);
			em.BlankLine();

			var i = em.BoundVar(em.Int.Type, "i");
			em.BlankLine();

			em.CurrentScope.Reachable("ord", out fnc);
			em.Param(a);
			em.Call((FunctionInfo)fnc, 1, i);
			em.BlankLine();

			var c = em.AddConstant(13);
			var cmp = em.InstrSub_TempBound(i, c);
			em.GotoIfZero("loop", cmp);
			c = em.AddConstant(10);
			em.InstrSub(cmp, i, c);
			em.GotoIfZero("loop", cmp);
			em.BlankLine();

			c = em.AddConstant(32);
			em.InstrSub(cmp, i, c);
			em.GotoIfZero("END", cmp);
			em.BlankLine();

			em.CurrentScope.Reachable("print", out fnc);
			em.Param(a);
			em.Call((FunctionInfo)fnc, 1);
			em.BlankLine();

			em.CurrentScope.Reachable("printi", out fnc);
			em.Param(i);
			em.Call((FunctionInfo)fnc, 1);
			em.Goto("loop");
		}
	}
}
