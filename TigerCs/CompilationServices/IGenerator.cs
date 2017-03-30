using System.IO;
using TigerCs.Generation.AST;
using TigerCs.Generation.AST.Expressions;

namespace TigerCs.CompilationServices
{
	public interface IGenerator
	{
		IExpression Parse(TextReader input, ErrorReport tofill);

		void AddStd(MemberDefinition md, string bcm_name);

		void Compile(IExpression rootprogram, ErrorReport tofill);
	}
}
