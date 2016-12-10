using System;
using System.Collections.Generic;

namespace TigerCs.Emitters
{
	public abstract class EmitterScope<T>
		where T : EmitterScope<T>
	{
		public readonly Guid BeforeEnterScope, BiginScope, EndScope, AfterEndScope;
		public readonly T Parent;
		public readonly Dictionary<Guid, string> ScopeLabels;
		public readonly Dictionary<Guid, string> ExpectedLabels;

		protected EmitterScope(T parent, Guid bes, Guid bs, Guid es, Guid ae)
		{
			BeforeEnterScope = bes;
			BiginScope = bs;
			EndScope = es;
			AfterEndScope = ae;
			ScopeLabels = new Dictionary<Guid, string> {[bs] = "Begin", [es] = "End" };
			if (parent != null)
			{
				parent.ScopeLabels.Add(bes, "BeforeEnter");
				parent.ScopeLabels.Add(ae, "AfterEnd");
			}
			ExpectedLabels = new Dictionary<Guid, string>();
			Parent = parent;
		}
	}
}
