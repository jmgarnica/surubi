using System;
using System.Collections.Generic;
using TigerCs.Generation;
using TigerCs.Generation;
using System.Linq;
using TigerCs.CompilationServices;

namespace TigerCs.Emitters
{
	public class DefaultSemanticChecker : ISemanticChecker
	{
		#region [fields]
		Dictionary<string, MemberInfo> trappedSTD;
		TigerSemanticScope rootscope, currentscope;
		ErrorReport report;
		#endregion


		public bool DeclareMember(string name, MemberInfo member)
		{
			MemberInfo existent;
			if (Reachable(name, out existent)) return false;

			currentscope.Namespace[name] = member;
			if (member is TypeInfo) currentscope.ContainsTypeDefinitions = true;
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
			var newscope = new TigerSemanticScope { Parent = currentscope, Descriptors = descriptors, Closure = autoclosure };
			//currentscope.Children.Add(newscope);
			currentscope = newscope;
		}

		public void InitializeSemanticCheck(ErrorReport report, Dictionary<string, MemberInfo> trappedSTD = null)
		{
			this.report = report;
			this.trappedSTD = trappedSTD;
			rootscope = new TigerSemanticScope();
			currentscope = rootscope;
		}

		public void LeaveScope()
		{
			currentscope.Closure = null;
			currentscope = currentscope.Parent;
			if (currentscope == null) report.Add(new TigerStaticError { Level = ErrorLevel.Critical, ErrorMessage = "attempt to leave root scope" });
		}

		public bool Reachable(string name, out MemberInfo member, MemberDefinition desired = null)
		{
			member = null;
			var closures = new List<Dictionary<string, MemberInfo>>();
			var current = currentscope;
			do
			{
				if (current.Namespace.TryGetValue(name, out member))
					break;

				if (current.Closure != null) closures.Add(current.Closure);
				current = current.Parent;
			}
			while (current != null);

			#region [std trapped]
			if (trappedSTD != null && member == null)
			{
				if (trappedSTD.TryGetValue(name, out member))
				{
					if (desired != null)
					{
						if (!desired.GetType().Equals(member.GetType()))
						{
							report.Add(new TigerStaticError { Level = ErrorLevel.Error, ErrorMessage = "using before declaration :" + name });
							return false;
						}

						HolderInfo hm;
						FunctionInfo fm;
						TypeInfo tm;
						if ((hm = member as HolderInfo) != null)
						{
							var hd = desired as HolderInfo;
							if (!hm.Type.Equals(hd.Type))
							{
								report.Add(new TigerStaticError { Level = ErrorLevel.Error, ErrorMessage = string.Format("{0} using as holders of diferent types {1}, {2}", name, hm.Type, hd.Type) });
								return false;
							}
						}
						else if ((fm = member as FunctionInfo) != null)
						{
							var fd = desired as FunctionInfo;
							if (!fm.Return.Equals(fd.Return))
							{
								report.Add(new TigerStaticError { Level = ErrorLevel.Error, ErrorMessage = string.Format("{0} using as function of diferent return types {1}, {2}", name, fm.Return, fd.Return) });
								return false;
							}

							if (fm.Parameters.Count != fd.Parameters.Count)
							{
								report.Add(new TigerStaticError { Level = ErrorLevel.Error, ErrorMessage = string.Format("{0} using as function of diferent number of parameters {1}, {2}", name, fm.Parameters.Count, fd.Parameters.Count) });
								return false;
							}

							for (int i = 0; i < fm.Parameters.Count; i++)
							{
								if (!fm.Parameters[i].Item2.Equals(fd.Parameters[i].Item2))
								{
									report.Add(new TigerStaticError
									{
										Level = ErrorLevel.Error,
										ErrorMessage = string.Format(
										"in function {0} formal parameter ({5}) {1}: {2} difers in type from expected {3}: {4}",
										name,
										fm.Parameters[i].Item1,
										fm.Parameters[i].Item2,
										fd.Parameters[i].Item1,
										fd.Parameters[i].Item2,
										i)
									});
									return false;
								}
							}
						}
						else if ((tm = member as TypeInfo) != null)
						{
							var td = desired as TypeInfo;
							if ((tm.ArrayOf == null && td.ArrayOf != null) || (tm.ArrayOf != null && td.ArrayOf == null))
							{
								report.Add(new TigerStaticError { Level = ErrorLevel.Error, ErrorMessage = string.Format("using {0} to name array and non-array types", name) });
								return false;
							}
							else if (tm.ArrayOf != null && !tm.ArrayOf.Equals(td.ArrayOf))
							{
								report.Add(new TigerStaticError { Level = ErrorLevel.Error, ErrorMessage = string.Format("using {0} to name arrays of deferent types {1}, {2}", name, tm.ArrayOf, td.ArrayOf) });
								return false;
							}
							else if (tm.ArrayOf == null)
							{
								if (tm.Members.Count != td.Members.Count)
								{
									report.Add(new TigerStaticError { Level = ErrorLevel.Error, ErrorMessage = string.Format("in record {0} number of members difer from expected {1}, {2}", name, tm.Members.Count, td.Members.Count) });
									return false;
								}

								foreach (var item in td.Members)
								{
									TypeInfo i;
									if (!tm.Members.TryGetValue(item.Key, out i))
									{
										report.Add(new TigerStaticError
										{
											Level = ErrorLevel.Error,
											ErrorMessage = string.Format("mising member {0} in record {1}", item.Key, name)
										});
										return false;
									}
									if (!i.Equals(item.Value))
									{
										report.Add(new TigerStaticError
										{
											Level = ErrorLevel.Error,
											ErrorMessage = string.Format("type of member {0}: {2} in record {1} difers from expected {3}", item.Key, name, i, item.Value)
										});
										return false;
									}
								}
							}
						}
					}
				}
				else
				{
					report.Add(new TigerStaticError
					{
						Level = ErrorLevel.Error,
						ErrorMessage = string.Format("mising member {0}, an auto trapped std member with the same name exist", name)
					});
					return false;
				}

			}
			#endregion

			if (member == null) return false;

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
					if (current.Descriptors[i] is T) return (T)current.Descriptors[i];
					else if (stop(current.Descriptors[i])) return null;
				}
			} while (current != null);

			return null;
		}

	}
}
