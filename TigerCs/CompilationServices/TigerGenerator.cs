using System.Collections.Generic;
using TigerCs.Generation.ByteCode;
using TigerCs.Generation;
using TigerCs.Generation.AST.Expresions;

namespace TigerCs.CompilationServices
{
	public sealed class TigerGenerator<T, F, H>
		where T : class, IType<T, F>
		where F : class, IFunction<T, F>
		where H : class, IHolder
	{
		public ErrorReport Report { get; }

		readonly ISemanticChecker sc;
		readonly IByteCodeMachine<T,F,H> bcm;
		public TigerGenerator(ISemanticChecker checker, IByteCodeMachine<T, F, H> bcm)
		{
			sc = checker;
			this.bcm = bcm;
			Report = new ErrorReport();
		}

		public void Compile(IExpresion rootprogram)
		{
			var std = new Dictionary<string, MemberDefinition>();
			sc.InitializeSemanticCheck(Report, std);

			var main = new MAIN(rootprogram);

			if (!main.CheckSemantics(sc, Report)) return;

			bcm.InitializeCodeGeneration(Report);
			foreach (var m in std)
			{
				if (!m.Value.Member.BCMBackup) continue;
				if (m.Value.Member is TypeInfo)
				{
					T o;
					if (bcm.TryBindSTDType(m.Key, out o)) m.Value.Member.BCMMember = o;
				}
				else if (m.Value.Member is HolderInfo)
				{
					H o;
					if (bcm.TryBindSTDConst(m.Key, out o)) m.Value.Member.BCMMember = o;
				}
				else if (m.Value.Member is FunctionInfo)
				{
					F o;
					if (bcm.TryBindSTDFunction(m.Key, out o)) m.Value.Member.BCMMember = o;
				}

				if (!m.Value.Member.Bounded && m.Value.Generator == null)
				{
					Report.Add(new TigerStaticError { Level = ErrorLevel.Critical, ErrorMessage = "BCM does not have a definition for " + m.Key, Column = m.Value.column, Line = m.Value.column });
					return;
				}
			}

			foreach (var m in std)
			{
				if (!m.Value.Member.BCMBackup) continue;
				if (m.Value == null || m.Value.Generator != null) continue;
				m.Value.Generator.CheckSemantics(sc, Report);
				m.Value.Generator.GenerateCode(bcm, Report);
			}
			sc.End();

			main.GenerateCode(bcm, Report);

			main.ReleaseStaticData();
			bcm.End();
		}

	}

}
