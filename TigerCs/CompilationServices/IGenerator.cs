using TigerCs.Generation.AST.Expressions;

namespace TigerCs.CompilationServices
{
	public interface IGenerator
	{
		ErrorReport Report { get; }

		void Compile(IExpression rootprogram);
	}
}
