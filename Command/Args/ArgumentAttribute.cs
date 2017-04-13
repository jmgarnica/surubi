using System;

namespace Command.Args
{
	[AttributeUsage(AttributeTargets.Property)]
	public class ArgumentAttribute : Attribute
	{
		public int positionalargument;
		public bool flag;

		public ArgumentAttribute(int positionalargument = -1, bool flag = false)
		{
			this.positionalargument = positionalargument;
			this.flag = flag;
			if(flag && positionalargument > 0) throw new ArgumentException("Flag options cant be positional parameters");
		}

		public string ActionMethod { get; set; }
		public string ParseMethod { get; set; }
		public object DefaultValue { get; set; }
		public string Help { get; set; }
		public string OptionName { get; set; }
	}
}
