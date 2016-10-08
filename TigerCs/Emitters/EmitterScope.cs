using System;
using System.Collections.Generic;

namespace TigerCs.Emitters
{
	public abstract class EmitterScope<T>
		where T : EmitterScope<T>
	{
		public readonly Guid BeforeEnterScope, BiginScope, EndScope;
		public readonly T Parent;
		public readonly Dictionary<Guid, string> ScopeLabels;
		public readonly Dictionary<Guid, string> ExpectedLabels;

		protected EmitterScope(T parent, Guid bes, Guid bs, Guid es)
		{
			BeforeEnterScope = bes;
			BiginScope = bs;
			EndScope = es;
			ScopeLabels = new Dictionary<Guid, string> { [bes] = "BeforeEnter", [bs] = "Begin", [es] = "End" };
			ExpectedLabels = new Dictionary<Guid, string>();
		}
	}
}
