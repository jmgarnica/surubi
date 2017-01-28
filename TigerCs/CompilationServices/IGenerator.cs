using TigerCs.Generation.AST.Expresions;

namespace TigerCs.CompilationServices
{
	public interface IGenerator
	{
		ErrorReport Report { get; }

		void Compile(IExpresion rootprogram);
	}
}
