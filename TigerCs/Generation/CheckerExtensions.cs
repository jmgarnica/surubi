using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TigerCs.CompilationServices;
using TigerCs.Generation.AST.Declarations;
using TigerCs.Interpretation;

namespace TigerCs.Generation
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
			if (sc.Reachable(TypeInfo.MakeTypeName("void"), out Void, new MemberDefinition
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
			if (sc.Reachable(TypeInfo.MakeTypeName("Null"), out Null, new MemberDefinition
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

		public static FunctionInfo CompareString(this ISemanticChecker sc, ErrorReport report = null)
		{
			FunctionDeclaration fcmps = new FunctionDeclaration
			{
				//TODO: hacer algo con esto
			};

			MemberInfo cmps;
			var md = new MemberDefinition
			{
				Member = new FunctionInfo
				{
					Name = "print",
					Return = sc.Void(report),
					Parameters = new List<Tuple<string, TypeInfo>>
					{
						new Tuple<string, TypeInfo>("s", sc.String(report))
					}
				}
			};
			if (!sc.Reachable("print", out cmps, md))
			{
				report?.Add(new StaticError { Level = ErrorLevel.Internal, ErrorMessage = "Print STD function not defined" });
				return null;
			}
			return (FunctionInfo)cmps;
		}

		public static void ReleaseStaticData(this object o)
		{
			var i_props = o.GetType().GetProperties(BindingFlags.Instance|BindingFlags.Static).Where(p => p.GetCustomAttributes<ReleaseAttribute>().Any() && p.CanRead);
			foreach (var p in i_props)
			{
				var attr = p.GetCustomAttributes<ReleaseAttribute>();
				var release_attributes = attr as ReleaseAttribute[] ?? attr.ToArray();
				if (release_attributes.FirstOrDefault(r => r.Collection) != null && p.PropertyType.GetInterface("IEnumerable") != null)
				{
					IEnumerable ie = (IEnumerable)p.GetValue(o);
				}
				if (release_attributes.FirstOrDefault(r => !r.Collection) != null)
				{
					var op = p.GetValue(o);
					if (op == null) continue;
					op.ReleaseStaticData();
				}
			}
			var s_props = o.GetType().GetProperties(BindingFlags.Static).Where(p => p.GetCustomAttributes<ReleaseAttribute>().Any() && p.CanWrite);
			foreach (var p in s_props)
			{
				if (!p.PropertyType.IsByRef) continue;
				p.SetValue(o, null);
			}
		}
	}
}
