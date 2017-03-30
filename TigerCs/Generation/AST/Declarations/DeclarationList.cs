using System.Collections.Generic;
using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Declarations
{
	public interface IDeclarationList<out R> : IEnumerable<R>, IDeclaration
		where R: IDeclaration
	{
		int Count { get; }
		R this[int index] { get; }
	}

	/// <summary>
	/// Binds a collection of declarations to the CURRENT scope
	/// </summary>
	/// <typeparam name="R"></typeparam>
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

		public virtual bool BindName(ISemanticChecker sc, ErrorReport report)
		{
			foreach (var dex in this)
				if (!dex.BindName(sc, report)) return false;

			return true;
		}

		public virtual bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{
			Pure = true;

			foreach (var dex in this)
			{
				if (!dex.CheckSemantics(sc, report))
					return false;
			}
			return true;

		}

		public virtual void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
			where T : class, IType<T, F>
			where F : class, IFunction<T, F>
			where H : class, IHolder
		{
			foreach (var dex in this)
				dex.GenerateCode(cg,report);
		}
	}
}
