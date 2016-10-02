using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TigerCs.CompilationServices;
using TigerCs.Generation.AST.Declarations;

namespace TigerCs.Generation
{
	[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
	public class StaticDataAttribute : Attribute
	{ }

	[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
	public class ReleaseAttribute : Attribute
	{
		readonly bool c;
		public ReleaseAttribute(bool collection = false)
		{
			c = collection;
		}

		public bool Collection { get { return c; } }
	}

	public static class CheckerExtensions
	{
		public static TypeInfo Int(this ISemanticChecker sc, ErrorReport report = null)
		{
			MemberInfo Int;
			if (!sc.Reachable("int", out Int, new MemberDefinition { Member = new TypeInfo { Name = "int" } }))
			{
				if (report != null)
					report.Add(new TigerStaticError { Level = ErrorLevel.Critical, ErrorMessage = "Integer STD type not defined" });
				return null;
			}

			return (TypeInfo)Int;
		}

		public static TypeInfo String(this ISemanticChecker sc, ErrorReport report = null)
		{
			MemberInfo String;
			if (!sc.Reachable("string", out String, new MemberDefinition { Member = new TypeInfo { Name = "string" } }))
			{
				if (report != null)
					report.Add(new TigerStaticError { Level = ErrorLevel.Critical, ErrorMessage = "String STD type not defined" });
				return null;
			}

			return (TypeInfo)String;
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
			if (!sc.Reachable("void", out Void, new MemberDefinition { Member = new TypeInfo { Name = "void", BCMBackup = false } }))
			{
				if (report != null)
					report.Add(new TigerStaticError { Level = ErrorLevel.Critical, ErrorMessage = "Void STD type not defined" });
				return null;
			}

			return (TypeInfo)Void;
		}

		public static HolderInfo Nill(this ISemanticChecker sc, ErrorReport report = null)
		{
			MemberInfo Null;
			if (!sc.Reachable("Null", out Null, new MemberDefinition { Member = new TypeInfo { Name = "Null" } }))
			{
				if (report != null)
					report.Add(new TigerStaticError { Level = ErrorLevel.Critical, ErrorMessage = "Null STD type not defined" });
				return null;
			}

			MemberInfo Nill;
			if (!sc.Reachable("nill", out Nill, new MemberDefinition { Member = new HolderInfo { Name = "nill", Type = (TypeInfo)Null } }))
			{
				if (report != null)
					report.Add(new TigerStaticError { Level = ErrorLevel.Critical, ErrorMessage = "Nill STD const not defined" });
				return null;
			}

			return (HolderInfo)Nill;
		}

		#region [a bad night]
		//public static FunctionInfo Print(this ISemanticChecker sc, ErrorReport report = null)
		//{
		//	MemberInfo print;
		//	var md = new MemberDefinition
		//	{
		//		Member = new FunctionInfo
		//		{
		//			Name = "print",
		//			Return = sc.Void(report),
		//			Parameters = new List<System.Tuple<string, TypeInfo>> { new System.Tuple<string, TypeInfo>("s", sc.String(report)) }
		//		}
		//	};
		//          if (!sc.Reachable("print", out print, md))
		//	{
		//		if (report != null)
		//			report.Add(new TigerStaticError { Level = ErrorLevel.Critical, ErrorMessage = "Print STD function not defined" });
		//		return null;
		//	}
		//	return (FunctionInfo)print;
		//}

		//public static FunctionInfo Printi(this ISemanticChecker sc, ErrorReport report = null)
		//{
		//	MemberInfo printi;
		//	var md = new MemberDefinition
		//	{
		//		Member = new FunctionInfo
		//		{
		//			Name = "printi",
		//			Return = sc.Void(report),
		//			Parameters = new List<System.Tuple<string, TypeInfo>> { new System.Tuple<string, TypeInfo>("i", sc.Int(report)) }
		//		}
		//	};
		//	if (!sc.Reachable("printi", out printi, md))
		//	{
		//		if (report != null)
		//			report.Add(new TigerStaticError { Level = ErrorLevel.Critical, ErrorMessage = "Printi STD function not defined" });
		//		return null;
		//	}
		//	return (FunctionInfo)printi;
		//}

		//public static FunctionInfo Flush(this ISemanticChecker sc, ErrorReport report = null)
		//{
		//	MemberInfo flush;
		//	var md = new MemberDefinition
		//	{
		//		Member = new FunctionInfo
		//		{
		//			Name = "flush",
		//			Return = sc.Void(report),
		//			Parameters = new List<System.Tuple<string, TypeInfo>> ()
		//		}
		//	};
		//	if (!sc.Reachable("flush", out flush, md))
		//	{
		//		if (report != null)
		//			report.Add(new TigerStaticError { Level = ErrorLevel.Critical, ErrorMessage = "Flush STD function not defined" });
		//		return null;
		//	}
		//	return (FunctionInfo)flush;
		//}
		#endregion

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
					Parameters = new List<System.Tuple<string, TypeInfo>> { new System.Tuple<string, TypeInfo>("s", sc.String(report)) }
				}
			};
			if (!sc.Reachable("print", out cmps, md))
			{
				if (report != null)
					report.Add(new TigerStaticError { Level = ErrorLevel.Critical, ErrorMessage = "Print STD function not defined" });
				return null;
			}
			return (FunctionInfo)cmps;
		}

		public static void ReleaseStaticData(this object o)
		{
			var i_props = o.GetType().GetProperties(BindingFlags.Instance|BindingFlags.Static).Where(p => p.CustomAttributes.OfType<ReleaseAttribute>().Any() && p.CanRead);
			foreach (var p in i_props)
			{
				var attr = p.CustomAttributes.OfType<ReleaseAttribute>();
				if (attr.FirstOrDefault(r => r.Collection) != null && p.PropertyType.GetInterface("IEnumerable") != null)
				{   
					IEnumerable ie = (IEnumerable)p.GetValue(o);
				}
				if (attr.FirstOrDefault(r => !r.Collection) != null)
				{
					var op = p.GetValue(o);
					if (op == null) continue;
					op.ReleaseStaticData();
				}				
			}
			var s_props = o.GetType().GetProperties(BindingFlags.Static).Where(p => p.CustomAttributes.OfType<StaticDataAttribute>().Any() && p.CanWrite);
			foreach (var p in s_props)
			{
				if (!p.PropertyType.IsByRef) continue;
				p.SetValue(o, null);
			}
		}
	}
}
