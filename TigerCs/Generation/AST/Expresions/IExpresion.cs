using System;
using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Expresions
{
	public interface IExpresion : IDisposable, IASTNode
	{		
		TypeInfo Return { get; }
		HolderInfo ReturnValue { get; }
	}

	public abstract class Expresion : IExpresion
	{
		public int column { get; set; }
		public bool CorrectSemantics { get; protected set; }
		public string Lex { get; set; }
		public int line { get; set; }
		public TypeInfo Return { get; protected set; }
		public HolderInfo ReturnValue { get; protected set; }

		public abstract bool CheckSemantics(ISemanticChecker sc, ErrorReport report);

		/// <summary>
		/// Reset static data
		/// </summary>
		public virtual void Dispose()
		{
			//reset static data
		}

		public abstract void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
			where T : class, IType<T, F>
			where F : class, IFunction<T, F>
			where H : class, IHolder;
	}

	public static class ExpesionExtensions
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
		/// Void is an empty type for returns use int type in the bcm
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
	}
}