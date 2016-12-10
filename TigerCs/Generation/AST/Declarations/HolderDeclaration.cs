using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;
using TigerCs.Generation;
using TigerCs.Generation.AST;

namespace TigerCs.Generation.AST.Declarations
{
	public class HolderDeclaration : IDeclaration
	{
		public string HolderType { get; set; }

		public int column { get; set; }

		public int line { get; set; }

		public string Lex { get; set; }

		public bool CorrectSemantics { get; }

		public virtual void BindName(ISemanticChecker sc, ErrorReport report)
		{
			throw new NotImplementedException();
		}

		public virtual bool CheckSemantics(ISemanticChecker sc, ErrorReport report)
		{
			throw new NotImplementedException();
		}

		public virtual void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
			where T : class, IType<T, F>
			where F : class, IFunction<T, F>
			where H : class, IHolder
		{
			throw new NotImplementedException();
		}
	}
}