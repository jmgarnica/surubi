using System.Collections.Generic;
using TigerCs.Generation.ByteCode;
using TigerCs.Generation;
using TigerCs.Generation.AST.Expressions;

namespace TigerCs.CompilationServices
{
	public sealed class TigerGenerator<T, F, H> : IGenerator
		where T : class, IType<T, F>
		where F : class, IFunction<T, F>
		where H : class, IHolder
	{
		public ErrorReport Report { get; }

		public ISemanticChecker SemanticChecker { get; set; }

		public IByteCodeMachine<T, F, H> ByteCodeMachine { get; set; }

		public TigerGenerator()
		{
			Report = new ErrorReport();
		}

		public void Compile(IExpression rootprogram)
		{
			var std = new Dictionary<string, MemberDefinition>();
			SemanticChecker.InitializeSemanticCheck(Report, std);

			var main = new MAIN(rootprogram);

			if (!main.CheckSemantics(SemanticChecker, Report)) return;

			ByteCodeMachine.InitializeCodeGeneration(Report);
			foreach (var m in std)
			{
				if (!m.Value.Member.BCMBackup) continue;
				if (m.Value.Member is TypeInfo)
				{
					T o;
					if (ByteCodeMachine.TryBindSTDType(m.Value.Member.Name, out o)) m.Value.Member.BCMMember = o;
				}
				else if (m.Value.Member is HolderInfo)
				{
					H o;
					if (ByteCodeMachine.TryBindSTDConst(m.Value.Member.Name, out o)) m.Value.Member.BCMMember = o;
				}
				else if (m.Value.Member is FunctionInfo)
				{
					F o;
					if (ByteCodeMachine.TryBindSTDFunction(m.Value.Member.Name, out o)) m.Value.Member.BCMMember = o;
				}

				if (m.Value.Member.Bounded || m.Value.Generator != null) continue;
				Report.Add(new StaticError
				           {
					           Level = ErrorLevel.Internal,
					           ErrorMessage = $"BCM does not have a definition for {m.Key}",
					           Column = m.Value.column,
					           Line = m.Value.column
				           });
				return;
			}

			foreach (var m in std)
			{
				if (!m.Value.Member.BCMBackup) continue;
				if (m.Value?.Generator == null) continue;

				m.Value.Generator.CheckSemantics(SemanticChecker, Report);
				m.Value.Generator.GenerateCode(ByteCodeMachine, Report);
			}
			SemanticChecker.End();

			main.GenerateCode(ByteCodeMachine, Report);
			ByteCodeMachine.End();
		}

	}

}
