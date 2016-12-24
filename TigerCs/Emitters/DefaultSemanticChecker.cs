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

		public void InitializeSemanticCheck(ErrorReport report, Dictionary<string, MemberDefinition> trappedSTD = null)
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
			if (currentscope == null) report.Add(new StaticError { Level = ErrorLevel.Internal, ErrorMessage = "attempt to leave root scope" });
		}

		public bool Reachable(string name, out MemberInfo member, MemberDefinition desired = null)
		{
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

			#region [std trap]
			if (trappedSTD != null && member == null)
			{
				MemberDefinition std;
				if (trappedSTD.TryGetValue(name, out std))
				{
					member = std.Member;
					if (desired != null)
					{
						if (desired.GetType() != std.Member.GetType())
						{
							var error = new StaticError
							{
								Level = ErrorLevel.Error,
								ErrorMessage = "using before declaration :" + name
							};
							report.Add(error);
							return false;
						}

						HolderInfo hm;
						FunctionInfo fm;
						TypeInfo tm;
						if ((hm = std.Member as HolderInfo) != null)
						{
							var hd = desired.Member as HolderInfo;
							if (!hm.Type.Equals(hd.Type))
							{
								report.Add(new StaticError
								{
									Level = ErrorLevel.Error,
									ErrorMessage = $"{name} using as holders of different types {hm.Type}, {hd.Type}"
								});
								return false;
							}
						}
						else if ((fm = std.Member as FunctionInfo) != null)
						{
							var fd = desired.Member as FunctionInfo;
							if (!fm.Return.Equals(fd.Return))
							{
								report.Add(new StaticError { Level = ErrorLevel.Error, ErrorMessage =
										           $"{name} using as function of different return types {fm.Return}, {fd.Return}"
								           });
								return false;
							}

							if (fm.Parameters.Count != fd.Parameters.Count)
							{
								report.Add(new StaticError { Level = ErrorLevel.Error, ErrorMessage =
										           $"{name} using as function of different number of parameters {fm.Parameters.Count}, {fd.Parameters.Count}"
								           });
								return false;
							}

							for (int i = 0; i < fm.Parameters.Count; i++)
							{
								if (!fm.Parameters[i].Item2.Equals(fd.Parameters[i].Item2))
								{
									report.Add(new StaticError
									{
										Level = ErrorLevel.Error,
										ErrorMessage = string.Format(
										"in function {0} formal parameter ({5}) {1}: {2} differs in type from expected {3}: {4}",
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
						else if ((tm = std.Member as TypeInfo) != null)
						{
							var td = desired.Member as TypeInfo;
							if ((tm.ArrayOf == null && td.ArrayOf != null) || (tm.ArrayOf != null && td.ArrayOf == null))
							{
								report.Add(new StaticError { Level = ErrorLevel.Error, ErrorMessage =
										           $"using {name} to name array and non-array types"
								           });
								return false;
							}
							if (tm.ArrayOf != null && !tm.ArrayOf.Equals(td.ArrayOf))
							{
								report.Add(new StaticError { Level = ErrorLevel.Error, ErrorMessage =
										           $"using {name} to name arrays of deferent types {tm.ArrayOf}, {td.ArrayOf}"
								           });
								return false;
							}
							if (tm.ArrayOf == null)
							{
								if (tm.Members.Count != td.Members.Count)
								{
									report.Add(new StaticError { Level = ErrorLevel.Error, ErrorMessage =
											           $"in record {name} number of members differs from expected {tm.Members.Count}, {td.Members.Count}"
									           });
									return false;
								}

								foreach (var item in td.Members)
								{
									TypeInfo i;
									if (!tm.Members.TryGetValue(item.Item1, out i))
									{
										report.Add(new StaticError
										           {
											           Level = ErrorLevel.Error,
											           ErrorMessage = $"mising member {item.Item1} in record {name}"
										           });
										return false;
									}
									if (!i.Equals(item.Item2))
									{
										report.Add(new StaticError
										           {
											           Level = ErrorLevel.Error,
											           ErrorMessage = string.Format("type of member {0}: {2} in record {1} differs from expected {3}", item.Item1, name, i, item.Item2)
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
					report.Add(new StaticError
					{
						Level = ErrorLevel.Error,
						ErrorMessage = $"mising member {name}, an auto trapped std member with the same name exist"
					});
					return false;
				}

			}
			#endregion

			if (member == null) return false;
			if (desired != null && !member.Equals(desired.Member)) return false;

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
