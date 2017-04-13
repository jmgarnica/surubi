namespace Command.Args
{
	public class Tuple<R, T> : ITuple<R, T>
	{
		public Tuple(R a, T b)
		{
			Item1 = a;
			Item2 = b;
		}
		public R Item1 { get; set; }
		public T Item2 { get; set; }
	}
}