using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TigerCs.CompilationServices;
using TigerCs.Generation;
using TigerCs.Generation.AST;

namespace CMPTest.AST
{
	public abstract class BCMHandler
	{
		public abstract void GenerateCode(IASTNode node, ErrorReport report);

		public abstract string Run(string[] args, string testdata, out int exitcode, out string errorout);
	}


	public abstract class ASTTester
	{
		protected abstract BCMHandler GetBCM(string testname);
		protected abstract ISemanticChecker GetSemanticChecker(string testname);


	}
}
