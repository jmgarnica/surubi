using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.Semantic.AST
{
	public abstract class Declaration : Expresion
	{
		public string Name { get; set; }
		public abstract void BindName(ISemanticChecker sc, ErrorReport report);
	}
}