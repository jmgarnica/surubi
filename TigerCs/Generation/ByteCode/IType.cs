using TigerCs.Generation.Semantic.Scopes;

namespace TigerCs.Generation.ByteCode
{
	public interface IType
	{
		bool Array { get; }

		/// <summary>
		/// Gets the bounded function that will allocate a new object of this type
		/// </summary>
		FunctionInfo Allocation { get; }

		/// <summary>
		/// Gets the bounded function that will dealocate an object of this tipe
		/// </summary>
		FunctionInfo Deallocator { get; }

		bool Equal(IType ty);
	}
}
