using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TigerCs.Generation.AST.Expressions;
using System.IO;
using TigerCs.CompilationServices;

namespace TigerCs.Parser
{
    public interface IParser
    {
        IExpression Parse(TextReader tr, ErrorReport tofill);
    }
}
