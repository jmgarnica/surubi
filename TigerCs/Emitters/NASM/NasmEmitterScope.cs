using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TigerCs.Emitters.NASM
{
	public class NasmEmitterScope : EmitterScope
	{
		public readonly NasmEmitterScope Parent;
		public readonly Guid BeforeEnterScope, BiginScope, EndScope;

		public RegisterLock Lock { get; private set; }

		public NasmEmitterScope(NasmEmitterScope parent, Guid beforeenterlabel,  Guid biginlabel, Guid endlabel)
		{
			Parent = parent;
			BeforeEnterScope = beforeenterlabel;
			BiginScope = biginlabel;
			EndScope = endlabel;
		}

		public void WirteCloseCode(FormatWriter w)
		{
			throw new NotImplementedException();
		}

		public bool FunctionScope { get; private set; }
		
	}
}
