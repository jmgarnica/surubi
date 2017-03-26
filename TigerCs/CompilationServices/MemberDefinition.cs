using TigerCs.Generation;

namespace TigerCs.CompilationServices
{
	public class MemberDefinition
	{
		public MemberInfo Member { get; set; }

		public int column { get; set; }
		public int line { get; set; }

		//public IDeclaration Generator { get; set; } //TODO: rethink this
	}
}
