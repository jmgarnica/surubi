using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Declarations
{
	public class ArrayDeclaration : TypeDeclaration
	{
		public string ArrayOf { get; set; }

		public override bool BindName(ISemanticChecker sc, ErrorReport report)
		{
			MemberDefinition mem;
			if (sc.Reachable(TypeInfo.MakeTypeName(ArrayOf), out mem))
			{
				var tem = mem.Member as TypeInfo;
				if (tem != null)
				{
					Dependencies = tem.Complete ? new string[0] : new[] { ArrayOf };
					mem = new MemberDefinition
					{
						line = line,
						column = column,
						Member = new TypeInfo
						{
							ArrayOf = tem,
							Complete = tem.Complete,
							Name = TypeName
						}
					};
				}
				else
				{
					report.Add(new StaticError(line, column, $"The non-type member {mem.Member.Name} was declared in a type namespace", ErrorLevel.Internal));
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
				Dependencies = new[] { ArrayOf };
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

			var tem = mem.Member as TypeInfo;
			if (tem == null)
			{
				report.Add(new StaticError(line, column, "Type declaration have been override", ErrorLevel.Internal));
				return false;
			}
            if (tem.ArrayOf.Complete)
            {
	            tem.Complete = true;
				return true;
			}

			MemberInfo mef;
			TypeInfo _base;
			if (!sc.Reachable(TypeInfo.MakeTypeName(ArrayOf), out mef) || (_base = mef as TypeInfo)?.Complete != true)
			{
				report.Add(new StaticError(line, column, $"Type {ArrayOf} is unaccessible", ErrorLevel.Error));
				return false;
			}

			tem.ArrayOf = _base;
			tem.Complete = true;
			Type = tem;
			return true;
		}

		public override void DeclareType<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			Type.BCMMember = cg.DeclareType(TypeName);
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			cg.BindArrayType((T)Type.BCMMember, (T)Type.ArrayOf.BCMMember);
		}
	}
}