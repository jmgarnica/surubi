using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Emitters.CSharp
{
	public class CSharpType : IType
	{
		public CSharpType(string dotnetname, bool array = false)
		{
			DotNetName = dotnetname;
			Array = array;
		}
		public bool Array
		{
			get; set;
		}

		public string DotNetName { get; set; }

		public bool Equal(IType ty)
		{
			if (ty is CSharpType)
			{
				return ((CSharpType)ty).DotNetName == DotNetName;
			}
			return false;
		}
	}
}
