using System;
using System.Linq;
using System.Reflection;
using TigerCs.Generation;
using TigerCs.Generation.AST;
using TigerCs.Generation.AST.Expressions;
using TigerCs.Interpretation;
using MemberInfo = TigerCs.Generation.MemberInfo;
using TypeInfo = TigerCs.Generation.TypeInfo;

namespace TigerCs.CompilationServices.AutoCheck
{
	public static class CheckerExtensions
	{
		public static TypeInfo Int(this ISemanticChecker sc, ErrorReport report = null)
		{
			MemberInfo Int;
			if (sc.Reachable(TypeInfo.MakeTypeName("int"), out Int, new MemberDefinition {Member = new TypeInfo {Name = "int"}}))
				return (TypeInfo)Int;

			report?.Add(new StaticError
			            {
				            Level = ErrorLevel.Internal,
				            ErrorMessage = "Integer STD type not defined"
			            });
			return null;
		}

		public static TypeInfo String(this ISemanticChecker sc, ErrorReport report = null)
		{
			MemberInfo String;
			if (sc.Reachable(TypeInfo.MakeTypeName("string"), out String, new MemberDefinition {Member = new TypeInfo {Name = "string"}}))
				return (TypeInfo)String;

			report?.Add(new StaticError { Level = ErrorLevel.Internal, ErrorMessage = "String STD type not defined" });
			return null;
		}

		/// <summary>
		/// Void is an empty type, for returns use int type in the bcm
		/// </summary>
		/// <param name="sc"></param>
		/// <param name="report"></param>
		/// <returns></returns>
		public static TypeInfo Void(this ISemanticChecker sc, ErrorReport report = null)
		{
			MemberInfo Void;
			if (sc.Reachable(TypeInfo.MakeTypeName(MemberInfo.MakeCompilerName("void")), out Void, new MemberDefinition
			                 {
				                 Member = new TypeInfo
				                 {
					                 Name = MemberInfo.MakeCompilerName("void"),
					                 BCMBackup = false
				                 }
			                 }))
				return (TypeInfo)Void;

			report?.Add(new StaticError { Level = ErrorLevel.Internal, ErrorMessage = "Void STD type not defined" });
			return null;
		}

		public static TypeInfo Null(this ISemanticChecker sc, ErrorReport report = null)
		{
			MemberInfo Null;
			if (sc.Reachable(TypeInfo.MakeTypeName(MemberInfo.MakeCompilerName("null")), out Null, new MemberDefinition
			                 {
				                 Member = new TypeInfo
				                 {
					                 Name = MemberInfo.MakeCompilerName("null"),
									 BCMBackup = false
				                 }
			                 }))
				return (TypeInfo)Null;

			report?.Add(new StaticError {Level = ErrorLevel.Internal, ErrorMessage = "Null STD type not defined"});
			return null;
		}

		/// <summary>
		/// Dummy is an empty type, use only to let semantic check continue after a type error
		/// </summary>
		/// <param name="sc"></param>
		/// <param name="report"></param>
		/// <returns></returns>
		public static TypeInfo Dummy(this ISemanticChecker sc, ErrorReport report = null)
		{
			MemberInfo Dummy;
			if (sc.Reachable(TypeInfo.MakeTypeName(MemberInfo.MakeCompilerName("dummy")), out Dummy, new MemberDefinition
			                 {
				                 Member = new DummyType(sc, report)
			                 }))
				return (TypeInfo)Dummy;

			report?.Add(new StaticError { Level = ErrorLevel.Internal, ErrorMessage = "Dummy not supported" });
			return null;
		}

		public static TypeInfo GetType(this ISemanticChecker sc, string tname, ErrorReport report, int line, int column, bool dummy_on_fault = true, bool hide_errors = false)
		{
			TypeInfo t;
			MemberInfo memb;
			if (!sc.Reachable(TypeInfo.MakeTypeName(tname), out memb))
			{
				if (!hide_errors) report.Add(new StaticError(line, column, $"Unknown type {tname}", ErrorLevel.Error));
				t = dummy_on_fault? sc.Dummy(report) : null;
				return t;
			}

			t = memb as TypeInfo;
			if (t != null) return t;

			if (!hide_errors)
				report.Add(new StaticError(line, column, $"Non-Type member {memb} declared in a type namespace", ErrorLevel.Internal));
			return null;
		}

		public static HolderInfo Nil(this ISemanticChecker sc, ErrorReport report = null)
		{
			MemberInfo Nil;
			if (sc.Reachable("nil", out Nil,
			                 new MemberDefinition {Member = new HolderInfo {Name = "nil", Type = sc.Null(report), ConstValue = IntpObject.Null}}))
				return (HolderInfo)Nil;

			report?.Add(new StaticError { Level = ErrorLevel.Internal, ErrorMessage = "nil STD const not defined" });
			return null;
		}

	}

	public static class NodeChecker
	{
		public static bool AutoCheck(this IASTNode node, ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{
			return NullCheck(node, report) && SemanticCheck(node, sc, report,expected) && ReturnCheck(node,sc, report);
		}

		public static bool NullCheck(IASTNode target, ErrorReport report)
		{
			var notnullprops = from p in target.GetType().GetProperties()
			                   let r = new {p, a = p.GetCustomAttribute<NotNullAttribute>()}
			                   where r.a != null
			                   select r;

			bool result = true;
			foreach (var prop in notnullprops)
			{
				object val = prop.p.GetValue(target);
				StaticError e;
				if (val == null)
                {
	                e = new StaticError(target.line, target.column, $"Member {prop.p.Name} can not be null",
	                                        ErrorLevel.Internal);
					switch (prop.a.Action)
					{
						case OnError.Stop:
							report?.Add(e);
							return false;
						case OnError.StopAfterTest:
							report?.Add(e);
							result = false;
							continue;

						case OnError.ErrorButNotStop:
							report?.Add(e);
							continue;

						case OnError.Ingnore:
							continue;

						default:
							throw new ArgumentOutOfRangeException();
					}
				}

				if (prop.a.InvalidValues?.Contains(val) != true) continue;

				e = new StaticError(target.line, target.column,
				                        $"Member {prop.p.Name} can not have value {(val.Equals("")? "\"\"" : val)}",
				                        ErrorLevel.Internal);

				switch (prop.a.Action)
				{
					case OnError.Stop:
						report?.Add(e);
						return false;
					case OnError.StopAfterTest:
						report?.Add(e);
						result = false;
						continue;

					case OnError.ErrorButNotStop:
						report?.Add(e);
						continue;

					case OnError.Ingnore:
						continue;

					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			return result;
		}

		public static bool SemanticCheck(IASTNode node, ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{
			var checkedprops = from p in node.GetType().GetProperties()
			                   let r = new {p, a = p.GetCustomAttribute<SemanticCheckedAttribute>()}
			                   where
			                   r.a != null &&
			                   (r.p.PropertyType == typeof(IASTNode) ||
			                    r.p.PropertyType.GetInterfaces().Contains(typeof(IASTNode)))
			                   orderby r.a.CheckOrder
			                   select r;
			bool result = true;
			foreach (var prop in checkedprops)
			{
				var val = prop.p.GetValue(node) as IASTNode;
				if(val == null) continue;

				TypeInfo exp = ReturnType(prop.a.Expected, sc, report, prop.a.Dependency, node, expected);

				if(prop.a.NestedScope)sc.EnterNestedScope();

				var e = new StaticError(val.line, val.column,
				                        string.IsNullOrWhiteSpace(prop.a.FailMessage)
					                        ? $"Semantic Check of {prop.p.Name} fail"
					                        : prop.a.FailMessage, ErrorLevel.Error);

				if (!val.CheckSemantics(sc, report, exp))
				{
					if (prop.a.NestedScope) sc.LeaveScope();

					switch (prop.a.Action)
					{
						case OnError.Stop:
							report?.Add(e);
							return false;
						case OnError.StopAfterTest:
							report?.Add(e);
							result = false;
							continue;

						case OnError.ErrorButNotStop:
							report?.Add(e);
							continue;

						case OnError.Ingnore:
							continue;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}
				if (prop.a.NestedScope) sc.LeaveScope();
			}

			return result;
		}

		public static bool ReturnCheck(IASTNode node, ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{
			var retprops = from p in node.GetType().GetProperties()
							   let r = new { p, a = p.GetCustomAttribute<ReturnTypeAttribute>() }
						   where
						   r.a != null &&
						   (r.p.PropertyType == typeof(IExpression) ||
							r.p.PropertyType.GetInterfaces().Contains(typeof(IExpression)))
						   select r;
			bool result = true;
			StaticError e;
			foreach (var prop in retprops)
			{
				var val = prop.p.GetValue(node) as IExpression;
				if (val == null) continue;
				var rettype = val.Return;
				var type = prop.a.Return;

				if (rettype == null)
				{
					e = new StaticError(node.line, node.column, "AutoCheck: Unchecked member", ErrorLevel.Error);
                    switch (prop.a.Action)
					{
						case OnError.Stop:
							report?.Add(e);
							return false;

						case OnError.StopAfterTest:
							report?.Add(e);
							result = false;
							continue;

						case OnError.ErrorButNotStop:
							report?.Add(e);
							continue;

						case OnError.Ingnore:
							continue;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}

				bool arrayof = true;
				bool member = false;
				switch (type)
				{
					case ExpectedType.ArrayOfInt:
						type = ExpectedType.Int;
						rettype = rettype.ArrayOf;
						break;

					case ExpectedType.ArrayOfString:
						type = ExpectedType.String;
						rettype = rettype.ArrayOf;
						break;

					case ExpectedType.ArrayOfDependent:
						type = ExpectedType.Dependent;
						rettype = rettype.ArrayOf;
						break;

					case ExpectedType.ArrayOfExpected:
						type = ExpectedType.Expected;
						rettype = rettype.ArrayOf;
						break;

					case ExpectedType.MemberOfDependent:
						type = ExpectedType.Dependent;
						member = true;
						break;

					case ExpectedType.MemberOfExpected:
						type = ExpectedType.Expected;
						member = true;
						break;

					default:
						arrayof = false;
						break;
				}
				TypeInfo ret = ReturnType(type, sc, report, prop.a.Dependency, node, expected);
				ret = member? ret?.ArrayOf : ret;
				if (ret == null) continue;

				if (rettype == null)
				{
					e = new StaticError(node.line, node.column, $"AutoCheck: {val.Return} is not an array type", ErrorLevel.Error);
					switch (prop.a.Action)
					{
						case OnError.Stop:
							report?.Add(e);
							return false;

						case OnError.StopAfterTest:
							report?.Add(e);
							result = false;
							continue;

						case OnError.ErrorButNotStop:
							report?.Add(e);
							continue;

						case OnError.Ingnore:
							continue;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}

				if (rettype.Equals(ret)) continue;

				var _int = sc.Int(report);
				var _null = sc.Null(report);
				if (rettype != _int && ret != _int && (rettype == _null || ret == _null)) continue;

				e = new StaticError(node.line, node.column,
				                            $"{prop.p.Name}-expression[{val.Return}] must be of type {(arrayof? "array of " : "")}{ret}",
				                            ErrorLevel.Error);
				switch (prop.a.Action)
				{
					case OnError.Stop:
						report?.Add(e);
						return false;

					case OnError.StopAfterTest:
						report?.Add(e);
						result = false;
						continue;

					case OnError.ErrorButNotStop:
						report?.Add(e);
						continue;

					case OnError.Ingnore:
						continue;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			return result;
		}

		static TypeInfo ReturnType(ExpectedType type, ISemanticChecker sc, ErrorReport report,
			string dependency, IASTNode node, TypeInfo expected)
		{
			TypeInfo exp;
			#region [Expected Value]
			switch (type)
			{
				case ExpectedType.Unknown:
					exp = null;
					break;

				case ExpectedType.Int:
					exp = sc.Int(report);
					break;
				case ExpectedType.String:
					exp = sc.String(report);
					break;
				case ExpectedType.Void:
					exp = sc.Void(report);
					break;
				case ExpectedType.Null:
					exp = sc.Null(report);
					break;

				case ExpectedType.Dependent:
					if (string.IsNullOrEmpty(dependency)) goto case ExpectedType.Unknown;
					var depprop = node.GetType().GetProperty(dependency);
					if (depprop == null)
					{
						report?.Add(new StaticError(node.line, node.column, "AutoCheck: Missing dependency property", ErrorLevel.Warning));
						goto case ExpectedType.Unknown;
					}
					var deppropval = depprop.GetValue(node);
					if (deppropval == null)
					{
						report?.Add(new StaticError(node.line, node.column, "AutoCheck: Inaccessible dependency property", ErrorLevel.Warning));
						goto case ExpectedType.Unknown;
					}
					IExpression expression = deppropval as IExpression;
					exp = expression != null? expression.Return : deppropval as TypeInfo;
					if (exp == null)
					{
						if (expression == null)
							report?.Add(new StaticError(node.line, node.column,
							                            $"AutoCheck: Dependency property of incorrecte type, allowed types: {typeof(IExpression)}, {typeof(TypeInfo)}",
							                            ErrorLevel.Warning));
						else
							report?.Add(new StaticError(node.line, node.column,
							                            "AutoCheck: Unassigned dependency property",
							                            ErrorLevel.Warning));
						goto case ExpectedType.Unknown;
					}
					break;

				case ExpectedType.Expected:
					exp = expected;
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}
			#endregion
			return exp;
		}
	}
}
