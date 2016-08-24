using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Emitters.CSharp
{
	public class CSharpHolder : IHolder
	{
		public bool Assignable
		{
			get
			{
				throw new NotImplementedException();
			}
		}
	}
}
