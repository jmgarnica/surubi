using System.Collections.Generic;
using System.IO;
using TigerCs.Generation.AST;
using TigerCs.Generation.AST.Expressions;

namespace TigerCs.CompilationServices
{
	public interface IGenerator
	{
		IExpression Parse(TextReader input, ErrorReport tofill);

		void Compile(IExpression rootprogram, ErrorReport tofill, IDictionary<string, MemberDefinition> conststd = null);
	}
}
