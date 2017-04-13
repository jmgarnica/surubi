using System;

namespace Command.Args
{
	[AttributeUsage(AttributeTargets.Class| AttributeTargets.Struct)]
	public class ArgumentCandidateAttribute : Attribute
	{
		public string OptionName { get; set; }
		public string Help { get; set; }
		public Type[] Candidates { get; set; }

		public string AfterInitializationActionMethod { get; set; }
	}
}
