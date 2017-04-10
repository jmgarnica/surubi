﻿using System;
using System.Collections.Generic;
using TigerCs.Generation;
using TigerCs.CompilationServices;

namespace TigerCs.Emitters
{
	public class DefaultSemanticChecker : ISemanticChecker
	{
		#region [fields]
		IDictionary<string, MemberDefinition> trappedSTD, conststd;
		SemanticScope rootscope, currentscope;
		ErrorReport report;
		#endregion


		public bool DeclareMember(string name, MemberDefinition member, bool hideoutter = true)
		{
			if (conststd?.ContainsKey(name) == true) return false;

			if (!hideoutter)
			{
				MemberInfo existent;
				if (Reachable(name, out existent)) return false;
			}
			else if (currentscope.Namespace.ContainsKey(name)) return false;

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

		public void EnterNestedScope(IDictionary<string, MemberInfo> autoclosure = null, params object[] descriptors)
		{
			var newscope = new SemanticScope { Parent = currentscope, Descriptors = descriptors, Closure = autoclosure };
			//currentscope.Children.Add(newscope);
			currentscope = newscope;
		}

		public void InitializeSemanticCheck(ErrorReport report, IDictionary<string, MemberDefinition> conststd = null, IDictionary<string, MemberDefinition> trappedSTD = null)
		{
			this.report = report;
			this.trappedSTD = trappedSTD;
			this.conststd = conststd;
			rootscope = new SemanticScope();
			currentscope = rootscope;
		}

		public void LeaveScope(int count = 1)
		{
			while (count > 0)
			{
				currentscope.Closure = null;
				currentscope = currentscope.Parent;
				if (currentscope == null)
				{
					//report.Add(new StaticError { Level = ErrorLevel.Internal, ErrorMessage = "attempt to leave root scope" });
					return;
				}
				count--;
			}
		}

		public bool Reachable(string name, out MemberInfo member, MemberDefinition desired = null)
		{
			MemberDefinition md;
			bool tmp = Reachable(name, out md, desired);
			if (!tmp)
			{
				member = null;
				return false;
			}
			member = md.Member;
			return true;
		}

		public bool Reachable(string name, out MemberDefinition member, MemberDefinition desired = null)
		{
			member = null;
			MemberDefinition found;

			var closures = new List<IDictionary<string, MemberInfo>>();
			var current = currentscope;
			do
			{
				if (current.Namespace.TryGetValue(name, out found))
					break;

				if (current.Closure != null) closures.Add(current.Closure);
				current = current.Parent;
			}
			while (current != null);

			if (found == null)
				conststd?.TryGetValue(name, out found);

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

			member = found;

			foreach (var item in closures)
				item.Add(name, member.Member);

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
					if (stop != null && stop(current.Descriptors[i])) return null;
					var descriptor = current.Descriptors[i] as T;
					if (descriptor != null) return descriptor;
				}

				current = current.Parent;
			} while (current != null);

			return null;
		}

	}
}
