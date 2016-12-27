using System;
using System.Collections.Generic;
using TigerCs.Generation;
using TigerCs.CompilationServices;

namespace TigerCs.Emitters
{
	public class DefaultSemanticChecker : ISemanticChecker
	{
		#region [fields]
		Dictionary<string, MemberDefinition> trappedSTD;
		SemanticScope rootscope, currentscope;
		ErrorReport report;
		#endregion


		public bool DeclareMember(string name, MemberDefinition member)
		{
			MemberInfo existent;
			if (Reachable(name, out existent)) return false;

			currentscope.Namespace[name] = member;
			if (member.Member is TypeInfo) currentscope.ContainsTypeDefinitions = true;
			return true;
		}

		public void End()
		{
			rootscope = null;
			currentscope = null;
			trappedSTD = null;
			report = null;
		}

		public void EnterNestedScope(Dictionary<string, MemberInfo> autoclosure = null, params object[] descriptors)
		{
			var newscope = new SemanticScope { Parent = currentscope, Descriptors = descriptors, Closure = autoclosure };
			//currentscope.Children.Add(newscope);
			currentscope = newscope;
		}

		public void InitializeSemanticCheck(ErrorReport report, Dictionary<string, MemberDefinition> trappedSTD = null)
		{
			this.report = report;
			this.trappedSTD = trappedSTD;
			rootscope = new SemanticScope();
			currentscope = rootscope;
		}

		public void LeaveScope()
		{
			currentscope.Closure = null;
			currentscope = currentscope.Parent;
			if (currentscope == null) report.Add(new StaticError { Level = ErrorLevel.Internal, ErrorMessage = "attempt to leave root scope" });
		}

		public bool Reachable(string name, out MemberInfo member, MemberDefinition desired = null)
		{
			member = null;
			MemberDefinition found;

			var closures = new List<Dictionary<string, MemberInfo>>();
			var current = currentscope;
			do
			{
				if (current.Namespace.TryGetValue(name, out found))
					break;

				if (current.Closure != null) closures.Add(current.Closure);
				current = current.Parent;
			}
			while (current != null);

			if (trappedSTD != null && found == null)
				if (!trappedSTD.TryGetValue(name, out found) && desired != null)
				{
					trappedSTD.Add(name, desired);
					found = desired;
				}

			if (found == null) return false;
			if (desired != null && !found.Member.FillInconsistencyReport(desired.Member, report,
						found.line, found.column, desired.line, desired.column))
				return false;

			member = found.Member;

			foreach (var item in closures)
				item.Add(name, member);

			return true;
		}

		public T SeekDescriptor<T>(Predicate<object> stop = null)
			where T : class
		{
			var current = currentscope;
			do
			{
				for (int i = current.Descriptors.Length - 1; i >= 0; i--)
				{
					var descriptor = current.Descriptors[i] as T;
					if (descriptor != null) return descriptor;
					if (stop != null && stop(current.Descriptors[i])) return null;
				}

				current = current.Parent;
			} while (current != null);

			return null;
		}

	}
}
