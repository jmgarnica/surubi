using System;
using System.Collections.Generic;
using TigerCs.CompilationServices;
using TigerCs.CompilationServices.AutoCheck;
using TigerCs.Interpretation;

namespace TigerCs.Generation
{

	public abstract class MemberInfo
	{
		object bcmmember;

		public string Name { get; set; }

		public virtual bool Bounded { get; protected set; } = false;

		public object BCMMember
		{
			get
			{
				if (!BCMBackup) throw new InvalidOperationException("this member is not intended to be operative at byte code level");
					return bcmmember;
			}
			set
			{
				if (!BCMBackup) return;
				bcmmember = value;
				Bounded = true;
			}
		}

		//public bool Used { get; set; }

		/// <summary>
		/// When false cant be bounded to bcm objects
		/// </summary>
		public bool BCMBackup { get; set; } = true;

		public abstract bool FillInconsistencyReport(MemberInfo mem, ErrorReport report, int thisline, int thiscol,
		                                             int memline, int memcol);

		public static string MakeCompilerName(string name) => $"<cg>{name}<cg>";

		/// <summary>Returns a string that represents the current object.</summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return Name;
		}
	}

	public class HolderInfo : MemberInfo
	{
		public HolderInfo()
		{
			Name = "<tmp>";
		}
		public TypeInfo Type { get; set; }

		/// <summary>
		/// Set the apropiate value if it can be resolved at compilation time, null otherwise.
		/// </summary>
		public IntpObject ConstValue { get; set; }

		public bool Const { get; set; }

		public override bool FillInconsistencyReport(MemberInfo mem, ErrorReport report, int thisline, int thiscol, int memline, int memcol)
		{
			var hol = mem as HolderInfo;
			if (hol == null)
			{
				report.Add(new StaticError(memline, memcol, "Holder expected", ErrorLevel.Error));
				return false;
			}

			if (Type == hol.Type) return true;

			report.Add(new StaticError(memline, memcol, $"Previous declaration at ({thisline}, {thiscol}) " +
			                                            "differs in type", ErrorLevel.Error));
			return false;
		}
	}

	public class FunctionInfo : MemberInfo
	{
		[NotNull]
		public List<Tuple<string, TypeInfo>> Parameters { get; set; }

		public List<Tuple<string, MemberInfo>> Closure { get; set; }

		public bool Pure { get; set; }

		[NotNull]
		public TypeInfo Return { get; set; }

		/// <summary>Serves as the default hash function. </summary>
		/// <returns>A hash code for the current object.</returns>
		public override int GetHashCode()
		{
			return Return.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			var func = obj as FunctionInfo;
			if (func == null) return false;

			if (Return != func.Return) return false;

			if (Parameters.Count != func.Parameters.Count) return false;
			for (int i = 0; i < Parameters.Count; i++)
				if (Parameters[i].Item2 != func.Parameters[i].Item2) return false;

			return true;
		}

		public override bool FillInconsistencyReport(MemberInfo mem, ErrorReport report, int thisline, int thiscol,
		                                             int memline, int memcol)
		{
			var func = mem as FunctionInfo;
			if (func == null)
			{
				report.Add(new StaticError(memline, memcol, "Function expected",
				                           ErrorLevel.Error));
				return false;
			}

			if (Return != func.Return)
			{
				report.Add(new StaticError(memline, memcol, $"Previous definition at ({thisline}, {thiscol})" +
				                                            " differs in return type",
				                           ErrorLevel.Error));
				return false;
			}

			if (Parameters.Count != func.Parameters.Count)
			{
				report.Add(new StaticError(memline, memcol, $"Previous definition at ({thisline}, {thiscol})" +
				                                            " differs in arguments count",
										   ErrorLevel.Error));
				return false;
			}
			for (int i = 0; i < Parameters.Count; i++)
			{
				if (Parameters[i].Item2 == func.Parameters[i].Item2) continue;
				report.Add(new StaticError(memline, memcol,
				                           $"Previous definition at ({thisline}, {thiscol}) differs in the type" +
				                           $" of positional argument {i}: {func.Parameters[i].Item1}",
				                           ErrorLevel.Error));
				return false;
			}

			return true;
		}
	}

	public class TypeInfo : MemberInfo
	{
		static readonly GuidGenerator types;

		static TypeInfo()
		{
			types = new GuidGenerator();
		}

		public List<Tuple<string, TypeInfo>> Members { get; set; }

		public Guid TypeId { get; }

		/// <summary>
		/// Null for no-array types
		/// </summary>
		public TypeInfo ArrayOf { get; set; }

		public bool Complete { get; set; } = true;

		public TypeInfo()
		{
			TypeId = types.GNext();
		}

		public static bool operator ==(TypeInfo a, TypeInfo b)
		{
			return (a?.TypeId == null && b?.TypeId == null) || (a?.TypeId != null && a.Equals(b));
		}

		public static bool operator !=(TypeInfo a, TypeInfo b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return TypeId.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			var info = obj as TypeInfo;
			if (info == null) return false;

			return info.TypeId == TypeId;
		}

		/// <summary>Returns a string that represents the current object.</summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return $"{Name}<cp_id: {TypeId.MinToString()}>";
		}

		public static string MakeArrayName(string t) => $"<array_of>{t}<array_of>";
		public static string MakeTypeName(string t) => $"<type>{t}<type>";

		public override bool FillInconsistencyReport(MemberInfo mem, ErrorReport report, int thisline, int thiscol, int memline, int memcol)
		{
			var type = mem as TypeInfo;
			if (type == null)
			{
				report.Add(new StaticError(memline, memcol, "Type expected", ErrorLevel.Error));
				return false;
			}

			if (ArrayOf != null)
			{
				if (type.Members != null || type.ArrayOf == null)
				{
					report.Add(new StaticError(memline, memcol, $"Previous declaration at ({thisline}, {thiscol})" +
																" is an array alias",
											   ErrorLevel.Error));
					return false;
				}

				if (ArrayOf == type.ArrayOf) return true;
				report.Add(new StaticError(memline, memcol, $"Previous declaration at ({thisline}, {thiscol})" +
				                                            " differs in the type of it´s elemets",
				                           ErrorLevel.Error));
				return false;
			}

			if (Members == null) return true;

			if (type.ArrayOf != null || type.Members == null)
			{
				report.Add(new StaticError(memline, memcol, $"Previous declaration at ({thisline}, {thiscol})" +
				                                            " is a record",
				                           ErrorLevel.Error));
				return false;
			}

			if(Members.Count != type.Members.Count)
			{
				report.Add(new StaticError(memline, memcol, $"Previous declaration at ({thisline}, {thiscol})" +
				                                            " differs in members count",
				                           ErrorLevel.Error));
				return false;
			}

			for (int i = 0; i < Members.Count; i++)
			{
				if (Members[i].Item2 == type.Members[i].Item2) continue;

				report.Add(new StaticError(memline, memcol, $"Previous declaration at ({thisline}, {thiscol})" +
				                                            " differs in the type of member" +
				                                            $" {i}: {type.Members[i].Item1}",
				                           ErrorLevel.Error));
				return false;
			}

			return true;
		}
	}

	public class Alias : MemberInfo
	{
		public MemberInfo InternalInfo { get; protected set; }

		public Alias(MemberInfo internalinfo)
		{
			InternalInfo = internalinfo;
		}

		public override bool Bounded
		{
			get
			{
				return InternalInfo.Bounded;
			}

			protected set
			{
				throw new InvalidOperationException("bound the true member");
			}
		}

		public override bool FillInconsistencyReport(MemberInfo mem, ErrorReport report, int thisline, int thiscol, int memline, int memcol)
		{
			return InternalInfo.FillInconsistencyReport(mem, report, thisline, thiscol, memline, memcol);
		}
	}

}
