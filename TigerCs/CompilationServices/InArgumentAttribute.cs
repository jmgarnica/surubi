using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TigerCs.CompilationServices
{

	[System.AttributeUsage(AttributeTargets.Property, Inherited = false)]
	public sealed class InArgumentAttribute : Attribute
	{
		public object DefaultValue { get; set; }
		public string Comment { get; set; }
		public string ConsoleShortName { get; set; }	
	}
}
