using System;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Emitters.NASM
{
	public class NasmType : NasmMember, IType<NasmType, NasmFunction>
	{
		public static readonly NasmType Int;
		public static readonly NasmType String;
		const int typesize = 8;
		public readonly NasmRefType RefType;
		public readonly int AsRefSize;

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

		public NasmFunction Allocator { get; set; }

		public NasmFunction Deallocator
		{
			get
			{
				return NasmFunction.Free;
			}
		}

		public NasmFunction DynamicMemberReadAccess { get; set; }

		public NasmFunction DynamicMemberWriteAccess { get; set; }

		public void DealocateType(FormatWriter fw, NasmEmitterScope accedingscope) => NasmFunction.Free.Call(fw, null, accedingscope, this);

		public static void AlocateType(FormatWriter fw, Register target, NasmEmitterScope accedingscope, NasmEmitter bound)
		{
			var sp = bound.AddConstant(typesize);
			NasmFunction.Malloc.Call(fw, target, accedingscope, sp);
		}
	}

	public enum NasmRefType
	{
		None,
		Fixed,
		Dynamic
	}
}
