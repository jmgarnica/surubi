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
		public IType Type { get; set; }

		public CSharpHolder Nested { get; set; }

		public string access { get; set; }

		public bool Assignable
		{
			get; set;
		}
	}
}
