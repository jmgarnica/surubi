using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TigerCs.CompilationServices;
using TigerCs.CompilationServices.AutoCheck;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Expressions
{
    public class RecordCreation:Expression
    {
		[NotNull]
        public List<Tuple<string, IExpression>> Members { get; set; }

        [NotNull]
        public string Name { get; set; }


        public override bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
        {
            return true;
        }

        public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
        {
        }
    }
}