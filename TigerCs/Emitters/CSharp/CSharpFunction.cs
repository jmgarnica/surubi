using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Emitters.CSharp
{
	public class CSharpFunction : IFunction
	{
		public CSharpFunction(string access, IType Return,bool bounded = true)
		{
			this.access = access;
			this.Return = Return;
			Bounded = bounded;
		}

		public CSharpFunction(IType Return)
		{
			this.Return = Return;
			Bounded = false;
		}

		public bool Bounded
		{
			get; set;
		}

		public string access { get; set; }

		public IType Return
		{
			get; set;
		}
	}
}
