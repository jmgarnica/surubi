namespace TigerCs.Generation.ByteCode
{
	public interface IMember
	{
	}

	public interface IFunction<T, F> : IMember
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

	public interface IType<T, F> : IMember
		where F : class, IFunction<T, F>
		where T : class, IType<T, F>
	{
		bool Array { get; }

		/// <summary>
		/// Gets the bounded function that will allocate a new object of this type
		/// Array Case
		/// parameters:
		/// IHolder : int -> element count
		/// IHolder : ArrayType -> default value
		/// returns :
		/// IHolder -> array
		/// 
		/// Record Case
		/// parameters:
		/// IHolders one per formal argumert
		/// returns:
		/// IHolder -> record
		/// </summary>
		F Allocation { get; }

		/// <summary>
		/// Gets the bounded function that will dealocate an object of this tipe
		/// parameters:
		/// IHolder -> instance
		/// returns: void
		/// </summary>
		F Deallocator { get; }

		/// <summary>
		/// Null if Array is false,
		/// parameters:
		/// IHolder : int -> element index
		/// returns:
		/// IHolder : ArrayType -> element
		/// </summary>
		F ArrayAccess { get; }
	}
}
