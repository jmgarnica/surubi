using System;
using System.Collections.Generic;
using System.Linq;
using TigerCs.CompilationServices;
using TigerCs.CompilationServices.AutoCheck;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Declarations
{
	public class RecordDeclaration : TypeDeclaration
	{
		/// <summary>
		/// Pairs (member_name, member_type)
		/// </summary>
		[NotNull(Action = OnError.ErrorButNotStop)]
		public List<Tuple<string, string>> Members { get; set; }

		List<Tuple<string, TypeInfo>> members;

		public override bool BindName(ISemanticChecker sc, ErrorReport report, List<string> same_scope_definitions = null)
		{
			if (!this.AutoCheck(sc, report)) return false;

			members = new List<Tuple<string, TypeInfo>>(Members.Count);
			bool complete = true;
			foreach (var t in Members)
			{
				var b = same_scope_definitions?.Contains(t.Item1) == true
					        ? null
					        : sc.GetType(t.Item1, report, line, column, false, true);
				members.Add(new Tuple<string, TypeInfo>(t.Item1, b));
				if (b == null) complete = false;
			}

			if (!sc.DeclareMember(TypeInfo.MakeTypeName(TypeName),
			                      new MemberDefinition
			                      {
				                      column = column,
				                      line = line,
				                      Member = DeclaredType = new TypeInfo
				                               {
					                               Members = members,
					                               Name = TypeName,
					                               Complete = complete
				                               }
			                      }))
				report.Add(new StaticError(line, column, $"This scope already contains a definition for {TypeName}", ErrorLevel.Error));
			Dependencies = new string[0];
			return true;
		}

		public override bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{
			if (DeclaredType.Complete) return true;

			for (int i = 0; i < Members.Count; i++)
			{
				if(members[i].Item2 != null) continue;
				TypeInfo t = sc.GetType(Members[i].Item2, report, line, column);
				members[i] = new Tuple<string, TypeInfo>(members[i].Item1, t);
			}
			DeclaredType.Complete = true;
			return true;
		}

		public override void DeclareType<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			DeclaredType.BCMMember = cg.DeclareType(TypeName);
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			cg.BindRecordType((T)DeclaredType.BCMMember,
			                  (from ts in DeclaredType.Members
			                   select new Tuple<string, T>(ts.Item1, (T)ts.Item2.BCMMember)).ToArray());
		}
	}
}