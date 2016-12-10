using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Expresions
{
	/// <summary>
	/// Represents a 
	/// </summary>
	public interface ILValue : IExpresion
	{
		/// <summary>
		/// Generate the code for setting the Return Holder's value of the espresion.
		/// </summary>		
		/// <param name="cg"></param>
		/// <param name="source"></param>
		/// <param name="report"></param>
		void SetValue<T, F, H>(IByteCodeMachine<T, F, H> cg, H source, ErrorReport report)
			where T : class, IType<T, F>
			where F : class, IFunction<T, F>
			where H : class, IHolder;
	}
}
