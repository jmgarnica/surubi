using System;
using System.Collections.Generic;
using TigerCs.CompilationServices;
using TigerCs.Generation.AST.Expresions;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Declarations
{
	public class FunctionDeclaration : IDeclaration
	{
		[Release]
		public IExpresion Body { get; set; }
		[Release(collection: true)]
		public List<ParameterDeclaration> Parameters { get; set; }
		public FunctionInfo Func { get; private set; }

		public int column {	get; set; }

		public int line	{ get; set;	}

		public string Lex {	get; set; }

		public bool CorrectSemantics { get; private set; }

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
