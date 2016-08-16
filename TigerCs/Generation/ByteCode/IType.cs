namespace TigerCs.Generation.ByteCode
{
	public interface IType
	{
		bool Array { get; }

		bool Equal(IType ty);
	}
}
