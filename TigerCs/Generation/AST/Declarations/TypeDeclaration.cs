using System;
using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Declarations
{
	public class TypeDeclaration : IDeclaration
	{
		public int column { get; set; }

		public int line { get; set; }

		public string Lex { get; set; }

		public bool CorrectSemantics { get; private set; }

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