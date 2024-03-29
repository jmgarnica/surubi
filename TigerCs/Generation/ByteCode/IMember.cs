﻿namespace TigerCs.Generation.ByteCode
{
	public interface IMember
	{
	}

	public interface IFunction<out T, F> : IMember
		where F : class, IFunction<T, F>
		where T : class, IType<T, F>
	{
		T Return { get; }

		bool Bounded { get; }
	}

	public interface IHolder : IMember
	{
		bool Assignable { get; }
	}

	public interface IType<T, out F> : IMember
		where F : class, IFunction<T, F>
		where T : class, IType<T, F>
	{
		/// <summary>
		/// Gets the bounded function that will allocate a new object of this type
		/// Array Case
		/// parameters:
		/// (0)IHolder : int -> element count
		/// (1)IHolder : ArrayType -> default value
		/// returns :
		/// IHolder -> array
		/// 
		/// Record Case
		/// parameters:
		/// IHolders one per formal argumert
		/// returns:
		/// IHolder -> record
		/// </summary>
		F Allocator { get; }

		/// <summary>
		/// Gets the bounded function that will dealocate an object of this type
		/// parameters:
		/// IHolder -> instance
		/// returns:
		/// void
		/// </summary>
		F Deallocator { get; }

		/// <summary>
		/// parameters:
		/// (0)IHolder : instance
		/// (1)IHolder : int -> element index
		/// returns:
		/// member value
		/// 
		/// throws IndexOutOfRange Error
		/// </summary>
		F DynamicMemberReadAccess { get; }

		/// <summary>
		/// parameters:
		/// (0)IHolder : instance
		/// (1)IHolder : int -> element index
		/// (2)IHolder : source
		/// returns:
		/// void
		/// 
		/// throws IndexOutOfRange Error
		/// </summary>
		F DynamicMemberWriteAccess { get; }
	}
}
