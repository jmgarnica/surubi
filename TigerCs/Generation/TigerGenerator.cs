using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TigerCs.Generation.ByteCode;
using TigerCs.Generation.Semantic;
using TigerCs.Generation.Semantic.AST;

namespace TigerCs.Generation
{
	public sealed class TigerGenerator<T, F, H>
		where T : class, IType<T, F>
		where F : class, IFunction<T, F>
		where H : class, IHolder
	{
		ISemanticChecker sc;
		IByteCodeMachine<T,F,H> bcm;
		public TigerGenerator(ISemanticChecker checker, IByteCodeMachine<T, F, H> bcm)
		{
			sc = checker;
			this.bcm = bcm;
		}

		public CompilationProces Compile(Expresion rootprogram)
		{
			ErrorReport er = new ErrorReport();
			var t = Task.Factory.StartNew(() =>
			{
				var std = new Dictionary<string, MemberInfo>();
				sc.InitializeSemanticCheck(er, std);
				if (rootprogram.CheckSemantics(sc, er))
				{
					sc.End();
					bcm.InitializeCodeGeneration(er);
					foreach (var item in std)
					{
						if (item.Value is TypeInfo)
							(item.Value as TypeInfo).Type = bcm.STDBoundType(item.Key);

						if (item.Value is FunctionInfo)
							(item.Value as FunctionInfo).Function = bcm.STDBoundFunction(item.Key);

						if (item.Value is HolderInfo)
							(item.Value as HolderInfo).Holder = bcm.STDBoundConst(item.Key);
					}

					rootprogram.GenerateCode(bcm, er);
					bcm.End();
				}
			});

			return new CompilationProces(er, t);
		}
	}

	public class CompilationProces
	{
		public ErrorReport Report { get; private set; }
		public bool Complete { get; private set; }
		public event Action CompilationComplete;

		Task compilation;
		public CompilationProces(ErrorReport report, Task compilation)
		{
			Report = report;
			Complete = false;
			compilation = compilation.ContinueWith((t) =>
			{
				Complete = true;
				if (CompilationComplete != null) CompilationComplete();
			});
		}
	}
}
