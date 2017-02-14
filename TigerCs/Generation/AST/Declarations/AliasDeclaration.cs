﻿using System;
using System.Diagnostics;
using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Declarations
{
	public class AliasDeclaration : TypeDeclaration
	{
		public string AliasOf { get; set; }

		public override bool BindName(ISemanticChecker sc, ErrorReport report)
		{
			MemberDefinition mem;
			if (sc.Reachable(TypeInfo.MakeTypeName(AliasOf), out mem))
			{
				var tem = mem.Member as TypeInfo;
				if (tem != null)
				{
					Dependencies = tem.Complete? new string[0] : new[] { AliasOf };
				}
				else
				{
					report.Add(new StaticError(line,column, $"The non-type member {mem.Member.Name} was declared in a type namespace", ErrorLevel.Internal));
					return false;
				}
			}
			else
			{
				mem = new MemberDefinition
				{
					line = line,
					column = column,
					Member = new TypeInfo
					{
						Name = TypeName,
						Complete = false
					}
				};
				Dependencies = new[] { AliasOf };
			}

			if (sc.DeclareMember(TypeInfo.MakeTypeName(TypeName), mem)) return true;

			report.Add(new StaticError(line, column, "A type with the same name already exist", ErrorLevel.Error));
			return false;
		}

		public override bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{
			MemberDefinition mem;
			if (!sc.Reachable(TypeInfo.MakeTypeName(TypeName), out mem))
			{
				report.Add(new StaticError(line, column, "Binding phase required", ErrorLevel.Internal));
				return false;
			}

			if ((Type = mem.Member as TypeInfo)?.Complete == true) return true;

			MemberInfo mef;
			if (!sc.Reachable(TypeInfo.MakeTypeName(AliasOf), out mef) || (Type = mef as TypeInfo)?.Complete != true)
			{
				report.Add(new StaticError(line, column, $"Type {AliasOf} is unaccessible", ErrorLevel.Error));
				return false;
			}

			mem.Member = mef;
			return true;
		}

		public override void DeclareType<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{ }

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{ }
	}
}