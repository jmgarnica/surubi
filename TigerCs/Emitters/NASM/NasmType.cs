using System;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Emitters.NASM
{
	public class NasmType : IType<NasmType, NasmFunction>
	{
		public static NasmType Int { get; set; }
		public static NasmType String { get; set; }
		public static NasmFunction QuadWordRMemberAccess { get; set; }
		public static NasmFunction ByteRMemberAccess { get; set; }
		public static NasmFunction QuadWordWMemberAccess { get; set; }
		public static NasmFunction ArrayAllocator { get; set; }
		public static NasmFunction ByteZeroEndArrayAllocator { get; set; }

		const int typesize = 4;
		public NasmRefType RefType { get; set; }
		public int AsRefSize { get; set; }
		public readonly string Name;
		public NasmType(NasmRefType reff, int asrefsize = 0, string name = "")
		{
			RefType = reff;
			switch (RefType)
			{
				case NasmRefType.NoSet:
				case NasmRefType.None: AsRefSize = 0;
					break;
				case NasmRefType.Fixed:
					if (asrefsize < 0) throw new ArgumentException("size must be greater or equal than 0");
					AsRefSize = asrefsize;
					break;
				case NasmRefType.Dynamic:
					AsRefSize = -1;
					break;
				default:
					throw new ArgumentException("unknow reference type");
			}
			Name = name;
		}

		public NasmFunction Deallocator
		{
			get
			{
				return NasmFunction.Free;
			}
		}

		/// <summary> 
		/// parameters:
		/// IHolder : int -> element index
		/// IHolder : instance
		/// returns:
		/// member value
		/// 
		/// throws IndexOutOfRange Error
		/// </summary>
		public NasmFunction DynamicMemberReadAccess
		{
			get
			{
				if (ReferenceEquals(this, String)) return ByteRMemberAccess;
				return RefType != NasmRefType.None ? QuadWordRMemberAccess : null;
			}
		}

		/// <summary> 
		/// parameters:
		/// IHolder : source
		/// IHolder : int -> element index
		/// IHolder : instance
		/// returns:
		/// void
		/// 
		/// throws IndexOutOfRange Error
		/// </summary>
		public NasmFunction DynamicMemberWriteAccess
		{
			get
			{
				if (ReferenceEquals(this, String)) return null;
				return RefType != NasmRefType.None ? QuadWordWMemberAccess : null;
			}
		}

		NasmFunction recorallocator;
		public NasmFunction Allocator
		{
			get
			{
				if (Equals(String)) return ByteZeroEndArrayAllocator;
				if (RefType == NasmRefType.Dynamic) return ArrayAllocator;
				return recorallocator;
			}
			set
			{
				if (RefType != NasmRefType.Fixed || RefType != NasmRefType.NoSet) throw new InvalidOperationException("Cant override default allocator");
				recorallocator = value;
			}
		}

	}

	public enum NasmRefType
	{
		None,
		Fixed,
		Dynamic,
		NoSet
	}
}
