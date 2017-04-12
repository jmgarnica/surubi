using System.Collections.Generic;
using System.Net.Security;
using System.Xml;
using TigerCs.CompilationServices;
using TigerCs.CompilationServices.AutoCheck;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Declarations
{
	public class AliasDeclaration : TypeDeclaration
	{
		[NotNull("")]
		public string AliasOf { get; set; }

		public override bool BindName(ISemanticChecker sc, ErrorReport report, List<string> same_scope_definitions = null)
		{
			if (!this.AutoCheck(sc, report)) return false;

			if(same_scope_definitions?.Contains(AliasOf) != true)
				DeclaredType = sc.GetType(AliasOf, report, line, column, false, true);

			if (DeclaredType == null) Dependencies = new[] {AliasOf};
			else
			{
				Dependencies = new string[0];
				if (!sc.DeclareMember(TypeInfo.MakeTypeName(TypeName),
									  new MemberDefinition { line = line, column = column, Member = DeclaredType }))
					report.Add(new StaticError(line, column, $"This scope already contains a definition for {TypeName}",
											   ErrorLevel.Error));
			}

			return true;
		}

		public override bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{
			if (DeclaredType != null) return true;
			DeclaredType = sc.GetType(AliasOf, report, line, column);

			if (!sc.DeclareMember(TypeInfo.MakeTypeName(TypeName),
									  new MemberDefinition { line = line, column = column, Member = DeclaredType }))
				report.Add(new StaticError(line, column, $"This scope already contains a definition for {TypeName}",
										   ErrorLevel.Error));
			return true;
		}

		public override void DeclareType<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{ }

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{ }
	}
}