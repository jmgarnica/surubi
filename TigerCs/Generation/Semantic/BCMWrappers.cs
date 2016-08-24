using System;
using System.Collections.Generic;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.Semantic
{

	public abstract class MemberInfo
	{
		public string Name { get; set; }

		public virtual bool Bounded { get; protected set; } = false;

	}

	public class HolderInfo : MemberInfo
	{
		public IHolder Holder { get; set; }

		public TypeInfo Type { get; set; }

	}

	public class FunctionInfo : MemberInfo
	{
		public IMember Function { get; set; }

		public List<Tuple<string, TypeInfo>> Parameters { get; set; }

		public List<Tuple<string, MemberInfo>> Closure { get; set; }

		public TypeInfo Return { get; set; }
	}

	public class TypeInfo : MemberInfo
	{
		public Dictionary<string, TypeInfo> Members { get; set; }

		/// <summary>
		/// Set on code generation phase, and used there only
		/// </summary>
		public Guid TypeId { get; set; }

		public IMember Type { get; set; }

		/// <summary>
		/// Null for no-array types
		/// </summary>
		public TypeInfo ArrayOf { get; set; }

		public TypeInfo()
		{
			Members = new Dictionary<string, TypeInfo>();
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
