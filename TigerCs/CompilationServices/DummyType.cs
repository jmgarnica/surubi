using System;
using System.Collections.Generic;
using TigerCs.CompilationServices.AutoCheck;
using TigerCs.Generation;

namespace TigerCs.CompilationServices
{
	public class DummyType : TypeInfo
	{
		readonly ISemanticChecker sc;
		readonly ErrorReport r;
		public DummyType(ISemanticChecker dummyfor, ErrorReport report)
		{
			sc = dummyfor;
			r = report;
			BCMBackup = false;
			//dummy propagation
			Name = MakeCompilerName("dummy");
			Members = new List<Tuple<string, TypeInfo>> {new Tuple<string, TypeInfo>("dummy", this)};
			ArrayOf = this;
		}

		/// <summary>Determines whether the specified object is equal to the current object.</summary>
		/// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
		/// <param name="obj">The object to compare with the current object. </param>
		public override bool Equals(object obj)
		{
			var i = obj as TypeInfo;
			if (i == null) return false;
			if (i.Equals(sc.Null(r))) return false;
			return !i.Equals(sc.Void(r));
		}

		/// <summary>Serves as the default hash function. </summary>
		/// <returns>A hash code for the current object.</returns>
		public override int GetHashCode()
		{
			return 0;
		}
	}
}
