using System;
using System.Collections.Generic;
using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Declarations
{
	public interface IDeclarationList<out R> : IEnumerable<R>, IDeclaration
		where R : IDeclaration
	{
		int Count { get; }
		R this[int index] { get; }
	}

	public class DeclarationList<R> : List<R>, IDeclarationList<R>
		where R : IDeclaration
	{
		public int column
		{
			get;

			set;
		}

		public bool CorrectSemantics
		{ get; set; }

		public bool Pure { get; protected set; }

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

		public bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
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
