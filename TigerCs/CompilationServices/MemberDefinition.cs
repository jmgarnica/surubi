using TigerCs.Generation;

namespace TigerCs.CompilationServices
{
	public class MemberDefinition
	{
		MemberInfo memb;
		ErrorReport r;
		bool @readonly;

		public MemberDefinition(MemberInfo readonly_memb = null,  ErrorReport r = null)
		{
			if (readonly_memb == null) return;
			@readonly = true;
			memb = readonly_memb;
			this.r = r;
		}

		public MemberInfo Member {
			get { return memb; }
			set
			{
				if (@readonly) r?.Add(new StaticError(line, column, $"Member {memb} can not be redefined", ErrorLevel.Error));
				else memb = value;
			}
		}

		public int column { get; set; }
		public int line { get; set; }

		//public IDeclaration Generator { get; set; } //TODO: rethink this
	}
}
