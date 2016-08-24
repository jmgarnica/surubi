using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Emitters.CSharp
{
	public class CSharpFunction : IFunction<CSharpType, CSharpFunction>
	{
		public bool Bounded
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public CSharpType Return
		{
			get
			{
				throw new NotImplementedException();
			}
		}
	}
}
