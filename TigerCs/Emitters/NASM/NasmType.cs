using System;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Emitters.NASM
{
	public class NasmType : NasmMember, IType<NasmType, NasmFunction>
	{
		public static NasmType Int { get; set; }
		public static NasmType String { get; set; }
		public static NasmFunction RMemberAccess { get; set; }
		public static NasmFunction WMemberAccess { get; set; }

		const int typesize = 4;
		public NasmRefType RefType { get; set; }
		public int AsRefSize { get; set; }

		public NasmType(NasmEmitterScope dscope, int sindex, NasmRefType reff, int asrefsize = 0)
			: base(dscope, sindex)
		{
			RefType = reff;
			switch (RefType)
			{
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
				return RefType != NasmRefType.None ? RMemberAccess : null;
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
				return RefType != NasmRefType.None ? WMemberAccess : null;
			}
		}

		public void DealocateType(FormatWriter fw, NasmEmitterScope accedingscope) => NasmFunction.Free.Call(fw, null, accedingscope, this);

		public static void AlocateType(FormatWriter fw, Register target, NasmEmitterScope accedingscope, NasmEmitter bound)
		{
			var sp = bound.AddConstant(typesize);
			NasmFunction.Malloc.Call(fw, target, accedingscope, sp);
		}

		public NasmFunction Allocator { get; set; }

	}

	public enum NasmRefType
	{
		None,
		Fixed,
		Dynamic,
		NoSet
	}
}
