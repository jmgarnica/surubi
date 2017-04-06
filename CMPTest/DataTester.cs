using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Surubi;
using TigerCs.CompilationServices;

namespace CMPTest
{
	[TestClass]
	public class DataTester
	{
		JsonBatchTest tests;
		TigerGeneratorDescriptor g;
		const string testsource = "test.json";

		[TestInitialize]
		public void LoadTests()
		{
			try
			{
				g = new Command.Args.ArgParse<TigerGeneratorDescriptor>().Activate("not_important");
			}
			catch (Exception e)
			{
				Console.WriteLine("fail to obtain a generator: {0}", e.Message);
				Assert.Fail();
			}

			if (File.Exists(testsource))
				try
				{
					using (var t = new StreamReader(new FileStream(testsource, FileMode.Open, FileAccess.Read)))
					{
						tests = JsonConvert.DeserializeObject<JsonBatchTest>(t.ReadToEnd());
						return;
					}
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}

			tests = new JsonBatchTest
			{
				correct = new JsonPositiveTest[0],
				fail = new JsonNegativeTest[0]
			};

			Console.WriteLine("empty test");
		}

		[TestMethod]
		public void Correct()
		{
			ErrorReport r = new ErrorReport();
			for (int index = 0; index < tests.correct.Length; index++)
			{
				Console.WriteLine();
				var test = tests.correct[index];
				if(test.name.StartsWith("!"))
				{
					Console.WriteLine($"Test {test.name.Substring(1)}({index + 1}/{tests.correct.Length}) skipped\n");
					continue;
				}
				Console.WriteLine($"Test {test.name}({index + 1}/{tests.correct.Length})\n");
				r.Clear();

				//TODO: line end
				var exp = g.Generator.Parse(new StringReader(test.code), r);
				Assert.AreNotEqual(exp, null);
				Assert.AreEqual(r.Count(), 0);

				g.Generator.Compile(exp, r);
				Assert.AreEqual(r.Count(), 0);

				int exp_code;
				string actual = g.BCM.Run(test.args, test.input, r, test.correctOutput == null, out exp_code);

				Assert.AreEqual(0, exp_code);
				if(test.correctOutput != null)
					Assert.AreEqual(test.correctOutput, actual);

				Console.WriteLine($"Test {test.name}({index+1}/{tests.correct.Length}) passed");
                Console.WriteLine(actual);
			}
		}

		[TestMethod]
		public void Fail()
		{
			ErrorReport r = new ErrorReport();
			for (int index = 0; index < tests.fail.Length; index++)
			{
				Console.WriteLine();
				var test = tests.fail[index];
				if (test.name.StartsWith("!"))
				{
					Console.WriteLine($"Test {test.name.Substring(1)}({index + 1}/{tests.fail.Length}) skipped\n");
					continue;
				}
				Console.WriteLine($"Test {test.name}({index + 1}/{tests.fail.Length})\n");
				r.Clear();

				var exp = g.Generator.Parse(new StringReader(test.code), r);
				if (test.failOn == Phase.Parse)
				{
					if(test.errors != null && test.errors.Length > 0)
						Assert.IsTrue(OneOf(r,test.errors));
					Console.WriteLine($"Test {test.name}({index + 1}/{tests.fail.Length}) passed");
					continue;
				}

				Assert.AreNotEqual(exp, null);
				Assert.AreEqual(r.Count(), 0);

				g.Generator.Compile(exp, r);
				if (test.failOn == Phase.SemanticCheck || test.failOn == Phase.CodeGeneration) //TODO: split this
				{
					if (test.errors != null && test.errors.Length > 0)
						Assert.IsTrue(OneOf(r, test.errors));
					Console.WriteLine($"Test {test.name}({index + 1}/{tests.fail.Length}) passed");
					continue;
				}

				Assert.AreEqual(r.Count(), 0);

				//TODO: add execution errors
				//int exp_code;
				//string actual = g.BCM.Run(test.args, test.input, r, test.correctOutput == null, out exp_code);

				//Assert.AreEqual(0, exp_code);
				//if (test.correctOutput != null)
				//	Assert.AreEqual(test.correctOutput, actual);
				//Console.WriteLine(actual);
			}
		}

		static bool OneOf(ErrorReport ss, JsonError[] possible)
		{
			foreach (var s in ss)
			{
				foreach (var error in possible)
				{
					if ((error.line != -1 && error.line != s.Line) || (error.column != -1 && error.column != s.Column)) continue;
					if (string.IsNullOrEmpty(error.error)) continue;

					Regex r = new Regex(error.error, RegexOptions.Compiled | RegexOptions.IgnoreCase);
					if (!r.Match(s.ErrorMessage).Success) continue;
					Console.WriteLine(s);
					return true;
				}
			}

			return false;
		}
	}
}
