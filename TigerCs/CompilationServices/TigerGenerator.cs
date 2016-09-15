using System;
using System.Collections.Generic;
using System.Linq;
using TigerCs.Generation.ByteCode;
using TigerCs.Generation.Semantic;
using TigerCs.Generation.Semantic.AST;

namespace TigerCs.CompilationServices
{
	public sealed class TigerGenerator<T, F, H>
		where T : class, IType<T, F>
		where F : class, IFunction<T, F>
		where H : class, IHolder
	{
		public ErrorReport Report { get; private set; }

		ISemanticChecker sc;
		IByteCodeMachine<T,F,H> bcm;
		public TigerGenerator(ISemanticChecker checker, IByteCodeMachine<T, F, H> bcm)
		{
			sc = checker;
			this.bcm = bcm;
			Report = new ErrorReport();
		}

		public void Compile(IExpresion rootprogram)
		{
			var std = new Dictionary<string, MemberInfo>();
			sc.InitializeSemanticCheck(Report, std);

			var main = new MAIN(rootprogram);

			if (!main.CheckSemantics(sc, Report)) return;
			sc.End();

			bcm.InitializeCodeGeneration(Report);
			foreach (var m in std)
			{

				var f = m.Value as FunctionInfo;
				if (m.Value is TypeInfo)
				{
					T o;
					if (bcm.TrySTDBoundType(m.Key, out o)) (m.Value as TypeInfo).BCMMember = o;
				}
				else if (m.Value is HolderInfo)
				{
					H o;
					if (bcm.TrySTDBoundConst(m.Key, out o)) (m.Value as HolderInfo).BCMMember = o;
				}
				else if (m.Value is FunctionInfo)
				{
					F o;
					if (bcm.TrySTDBoundFunction(m.Key, out o))
					{
						f.BackupDefintion = false;
						f.BCMMember = o;
					}
				}

				if (!m.Value.Bounded)
				{
					if (f == null || !f.BackupDefintion)
					{
						Report.Add(new TigerStaticError { Level = ErrorLevel.Critical, ErrorMessage = "BCM does not have a definition for " + m.Key });
						return;
					}
				}
			}

			foreach (var m in std)
			{
				var f = m.Value as FunctionInfo;
				if (f == null || !f.BackupDefintion) continue;
				f.BCMMember = bcm.AheadGlobalFunctionDeclaration(
					f.Name,
					(T)f.Return.BCMMember,
					f.Parameters.Select(g => Tuple.Create(g.Item1, (T)g.Item2.BCMMember)).ToArray());
			}

			main.GenerateCode(bcm, Report);

			main.Dispose();
			bcm.End();
		}
		
	}

}
