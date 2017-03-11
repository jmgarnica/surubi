using System;
using System.CodeDom;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
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
			if (sc.Reachable(TypeInfo.MakeTypeName("<cg> void <cg>"), out Void, new MemberDefinition
			                 {
				                 Member = new TypeInfo
				                 {
					                 Name = "void",
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
			if (sc.Reachable(TypeInfo.MakeTypeName("<cg> Null <cg>"), out Null, new MemberDefinition
			                 {
				                 Member = new TypeInfo
				                 {
					                 Name = "Null",
									 BCMBackup = false
				                 }
			                 }))
				return (TypeInfo)Null;

			report?.Add(new StaticError {Level = ErrorLevel.Internal, ErrorMessage = "Null STD type not defined"});
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

			foreach (var prop in notnullprops)
			{
				object val = prop.p.GetValue(target);
                if (val == null)
				{
					report?.Add(new StaticError(target.line, target.column, $"Member {prop.p.Name} can not be null", ErrorLevel.Internal));
					return false;
				}

				if (prop.a.InvalidValues?.Contains(val) != true) continue;

				report?.Add(new StaticError(target.line, target.column,
				                            $"Member {prop.p.Name} can not have value {(val.Equals("")? "\"\"" : val)}",
				                            ErrorLevel.Internal));
				return false;
			}

			return true;
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

			foreach (var prop in checkedprops)
			{
				var val = prop.p.GetValue(node) as IASTNode;
				if(val == null) continue;

				TypeInfo exp = ReturnType(prop.a.Expected, sc, report, prop.a.Dependency, node, expected);

				if(prop.a.NestedScope)sc.EnterNestedScope();
				if (!val.CheckSemantics(sc, report, exp) && !prop.a.IgnoreFail)
				{
					if (!string.IsNullOrEmpty(prop.a.FailMessage))
						report?.Add(new StaticError(val.line, val.column, prop.a.FailMessage, ErrorLevel.Error));
					if (prop.a.NestedScope) sc.LeaveScope();
					return false;
				}
				if (prop.a.NestedScope) sc.LeaveScope();
			}

			return true;
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

			foreach (var prop in retprops)
			{
				var val = prop.p.GetValue(node) as IExpression;
				if (val == null) continue;

				var ret = ReturnType(prop.a.Return, sc, report, prop.a.Dependency, node, expected);
				if(ret == null) continue;

				if (val.Return == null)
				{
					report?.Add(new StaticError(node.line, node.column, "AutoCheck: Inaccessible dependency property", ErrorLevel.Error));
					return false;
				}

				if(val.Return.Equals(ret)) continue;

				var _int = sc.Int(report);
				var _null = sc.Int(report);
				if (val.Return != _int && ret != _int && (val.Return == _null || ret == _null)) continue;

				report?.Add(new StaticError(node.line, node.column, $"{prop.p.Name}-expression[{val.Return}] must be of type {ret}", ErrorLevel.Error));
				return false;
			}

			return true;
		}

		static TypeInfo ReturnType(RetrurnType type, ISemanticChecker sc, ErrorReport report,
			string dependency, IASTNode node, TypeInfo expected)
		{
			TypeInfo exp;
			#region [Expected Value]
			switch (type)
			{
				case RetrurnType.Unknown:
					exp = null;
					break;

				case RetrurnType.Int:
					exp = sc.Int(report);
					break;
				case RetrurnType.String:
					exp = sc.String(report);
					break;
				case RetrurnType.Void:
					exp = sc.Void(report);
					break;
				case RetrurnType.Null:
					exp = sc.Null(report);
					break;

				case RetrurnType.Dependent:
					if (string.IsNullOrEmpty(dependency)) goto case RetrurnType.Unknown;
					var depprop = node.GetType().GetProperty(dependency);
					if (depprop == null)
					{
						report?.Add(new StaticError(node.line, node.column, "AutoCheck: Missing dependency property", ErrorLevel.Warning));
						goto case RetrurnType.Unknown;
					}
					var deppropval = depprop.GetValue(node) as IExpression;
					if (deppropval == null || deppropval.Return == null)
					{
						report?.Add(new StaticError(node.line, node.column, "AutoCheck: Inaccessible dependency property", ErrorLevel.Warning));
						goto case RetrurnType.Unknown;
					}
					exp = deppropval.Return;
					break;

				case RetrurnType.Expected:
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
