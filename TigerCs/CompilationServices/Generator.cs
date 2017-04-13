using System.Collections.Generic;
using TigerCs.Generation.ByteCode;
using TigerCs.Generation;
using TigerCs.Generation.AST.Expressions;
using System;
using System.IO;
using System.Linq;
using TigerCs.Parser;

namespace TigerCs.CompilationServices
{
	public sealed class Generator<T, F, H> : IGenerator
		where T : class, IType<T, F>
		where F : class, IFunction<T, F>
		where H : class, IHolder
	{
		public ISemanticChecker SemanticChecker { get; set; }

		public IParser Parser { get; set; }

		public IByteCodeMachine<T, F, H> ByteCodeMachine { get; set; }

		public void Compile(IExpression rootprogram, ErrorReport tofill, IDictionary<string, MemberDefinition> conststd = null)
		{
			var std = new Dictionary<string, MemberDefinition>();
			SemanticChecker.InitializeSemanticCheck(tofill,conststd, std);

			var main = new MAIN(rootprogram);

			if (!main.CheckSemantics(SemanticChecker, tofill) || tofill.Count() != 0) return;

			ByteCodeMachine.InitializeCodeGeneration(tofill);
			foreach (var m in conststd != null ? std.Union(conststd) : std)
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

				if (m.Value.Member.Bounded) continue;
				tofill.Add(new StaticError
				           {
					           Level = ErrorLevel.Internal,
					           ErrorMessage = $"BCM does not have a definition for {m.Key}",
					           Column = m.Value.column,
					           Line = m.Value.line
				           });
				return;
			}

			SemanticChecker.End();

			main.GenerateCode(ByteCodeMachine, tofill);
			ByteCodeMachine.End();
		}

		public IExpression Parse(TextReader input, ErrorReport tofill)
		{
			return Parser.Parse(input, tofill);
		}
	}

}
