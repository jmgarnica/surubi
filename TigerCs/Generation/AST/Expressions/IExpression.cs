using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Expressions
{
	public interface IExpression : IASTNode
	{
		/// <summary>
		/// The type of ReturnValue.
		/// Void => this expresion does not return a value
		/// Null => this expresion always returns nil
		/// </summary>
		TypeInfo Return { get; }

		/// <summary>
		/// when Return is "void" this should be null
		/// </summary>
		HolderInfo ReturnValue { get; }

		bool CanBreak { get; }
	}

	public abstract class Expression : IExpression
	{
		public int column { get; set; }
		public bool CorrectSemantics { get; protected set; }
		public bool Pure { get; protected set; }
		public string Lex { get; set; }
		public int line { get; set; }
		public TypeInfo Return { get; protected set; }
		public HolderInfo ReturnValue { get; protected set; }

		public bool CanBreak { get; protected set; }//TODO: Fix this everywhere

		public abstract bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null);

		public abstract void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
			where T : class, IType<T, F>
			where F : class, IFunction<T, F>
			where H : class, IHolder;
	}
}