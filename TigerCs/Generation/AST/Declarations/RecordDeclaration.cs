using System;
using System.Collections.Generic;
using System.Linq;
using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Declarations
{
	public class RecordDeclaration : TypeDeclaration
	{
		/// <summary>
		/// Pairs (member_name, member_type)
		/// </summary>
		[NotNull]
		public List<Tuple<string, string>> Members { get; set; }

		public override bool BindName(ISemanticChecker sc, ErrorReport report)
		{
			Dependencies = new string[0];
			Type = new TypeInfo
			{
				Name = TypeName,
				Members = new List<Tuple<string, TypeInfo>>(Members.Count),
				Complete = false
			};

			if (sc.DeclareMember(TypeInfo.MakeTypeName(TypeName), new MemberDefinition
			                     {
				                     line = line,
				                     column = column,
				                     Member = Type
			                     })) return true;
			report.Add(new StaticError(line, column, "A type with the same name already exist", ErrorLevel.Error));
			return false;
		}

		public override bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{
			var sorted = new List<string>(from ts in Members select ts.Item1);
			sorted.Sort();
			for (int i = 0; i < sorted.Count - 1; i++)
			{
				if (!sorted[i].Equals(sorted[i + 1])) continue;
				report.Add(new StaticError(line, column, $"Member {sorted[i]} was defined more than one time", ErrorLevel.Error));
				return false;
			}

			foreach (var member in Members)
			{
				MemberInfo mem;
				TypeInfo tem;
				if (!sc.Reachable(TypeInfo.MakeTypeName(member.Item2), out mem) || (tem = mem as TypeInfo) == null)
				{
					report.Add(new StaticError(line, column, $"Type {member.Item2} is unaccessible", ErrorLevel.Error));
					return false;
				}

				Type.Members.Add(new Tuple<string, TypeInfo>(member.Item1, tem));
			}

			Type.Complete = true;
			return true;
		}

		public override void DeclareType<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			Type.BCMMember = cg.DeclareType(TypeName);
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			cg.BindRecordType((T)Type.BCMMember,
			                  (from ts in Type.Members
			                   select new Tuple<string, T>(ts.Item1, (T)ts.Item2.BCMMember)).ToArray());
		}
	}
}