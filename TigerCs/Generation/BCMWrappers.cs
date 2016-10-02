using System;
using System.Collections.Generic;

namespace TigerCs.Generation
{

	public abstract class MemberInfo
	{
		private object bcmmember;

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
	}

	public class HolderInfo : MemberInfo
	{
		public TypeInfo Type { get; set; }

	}

	public class FunctionInfo : MemberInfo
	{
		public List<Tuple<string, TypeInfo>> Parameters { get; set; }

		public List<Tuple<string, MemberInfo>> Closure { get; set; }

		public TypeInfo Return { get; set; }
	}

	public class TypeInfo : MemberInfo
	{
		public Dictionary<string, TypeInfo> Members { get; set; }

		public Guid TypeId { get; set; }

		/// <summary>
		/// Null for no-array types
		/// </summary>
		public TypeInfo ArrayOf { get; set; }

		public TypeInfo()
		{
			Members = new Dictionary<string, TypeInfo>();
			TypeId = Guid.NewGuid();
		}

		public static bool operator ==(TypeInfo a, TypeInfo b)
		{
			return a.TypeId == b.TypeId;
		}

		public static bool operator !=(TypeInfo a, TypeInfo b)
		{
			return a.TypeId != b.TypeId;
		}

		public override int GetHashCode()
		{
			return TypeId.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj is TypeInfo)
			{
				return ((TypeInfo)obj).TypeId == TypeId;
			}
			return false;
		}

		public static string MakeArrayName(string t) => "<array> of " + t;
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
				throw new InvalidOperationException("bound the true type");
			}
		}
	}

}
