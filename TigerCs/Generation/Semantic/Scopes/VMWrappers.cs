using System;
using System.Collections.Generic;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.Semantic.Scopes
{

	public class MemberInfo
	{
		public string Name { get; set; }

		public bool Bounded { get; set; } = false;
	}

	public class HolderInfo : MemberInfo
	{
		public IHolder Holder { get; set; }

		public TypeInfo Type { get; set; }
	}

	public class FunctionInfo : MemberInfo
	{
		public IFunction Function { get; set; }

		public List<Tuple<string, TypeInfo>> Parameters { get; set; }

		TypeInfo Return { get; set; }
	}

	public class TypeInfo : MemberInfo
	{
		public Dictionary<string, TypeInfo> Members { get; set; }

		public Guid TypeId { get; private set; }

		public IType Type { get; set; }

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
	}
}
