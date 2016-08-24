using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Emitters.CSharp
{
	public class CSharpType : IType<CSharpType, CSharpFunction>
	{
		public CSharpFunction Allocation
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public bool Array
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public CSharpFunction ArrayAccess
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public CSharpFunction Deallocator
		{
			get
			{
				throw new NotImplementedException();
			}
		}
	}
}
