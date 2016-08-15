using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TigerCs.Generation.ByteCode
{
	public interface IFunction
	{
		IType Return { get; }

		bool Bounded { get; }
	}
}
