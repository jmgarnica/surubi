using System;
using System.IO;
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
				var test = tests.correct[index];
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
		{ }
	}
}
