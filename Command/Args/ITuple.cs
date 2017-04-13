namespace Command.Args
{
	public interface ITuple<out R, out T>
	{
		R Item1 { get; }
		T Item2 { get; }
	}
}
