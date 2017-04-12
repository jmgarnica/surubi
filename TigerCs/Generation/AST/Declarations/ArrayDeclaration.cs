using System.Collections.Generic;
using System.Xml;
using TigerCs.CompilationServices;
using TigerCs.CompilationServices.AutoCheck;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Declarations
{
	public class ArrayDeclaration : TypeDeclaration
	{
		[NotNull("")]
		public string ArrayOf { get; set; }

		public override bool BindName(ISemanticChecker sc, ErrorReport report, List<string> same_scope_definitions = null)
		{
			if (!this.AutoCheck(sc, report)) return false;

			var t = same_scope_definitions?.Contains(ArrayOf) == true
				        ? null
				        : sc.GetType(ArrayOf, report, line, column, false, true);

			if (t == null) Dependencies = new[] { ArrayOf };
			else
			{
				DeclaredType = new TypeInfo {ArrayOf = t, Name = TypeName};
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
			var t = sc.GetType(ArrayOf, report, line, column);

			DeclaredType = new TypeInfo { ArrayOf = t, Complete = true, Name = TypeName };

			if (!sc.DeclareMember(TypeInfo.MakeTypeName(TypeName),
									  new MemberDefinition { line = line, column = column, Member = DeclaredType }))
				report.Add(new StaticError(line, column, $"This scope already contains a definition for {TypeName}",
										   ErrorLevel.Error));
			return true;
		}

		public override void DeclareType<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			DeclaredType.BCMMember = cg.DeclareType(TypeName);
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			cg.BindArrayType((T)DeclaredType.BCMMember, (T)DeclaredType.ArrayOf.BCMMember);
		}
	}
}