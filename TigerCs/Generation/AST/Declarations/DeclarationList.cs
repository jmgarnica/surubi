using System;
using System.Collections.Generic;
using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Declarations
{
	public class DeclarationList : List<IDeclaration>, IDeclaration
	{
		public int column
		{
			get;

			set;
		}

		public bool CorrectSemantics
		{ get; set; }

		public string Lex
		{
			get;

			set;
		}

		public int line
		{
			get;

			set;
		}

		public void BindName(ISemanticChecker sc, ErrorReport report)
		{
			throw new NotImplementedException();
		}

		public bool CheckSemantics(ISemanticChecker sc, ErrorReport report)
		{
			throw new NotImplementedException();
		}

		public void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
			where T : class, IType<T, F>
			where F : class, IFunction<T, F>
			where H : class, IHolder
		{
			throw new NotImplementedException();
		}
	}
}
