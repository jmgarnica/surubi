using System;
using TigerCs.CompilationServices;
using TigerCs.CompilationServices.AutoCheck;
using TigerCs.Emitters;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Declarations
{
	public class ParameterDeclaration : IDeclaration
	{
		public int Position { get; set; }

		[NotNull("")]
		public string HolderType { get; set; }

		[NotNull("")]
		public string HolderName { get; set; }

		public HolderInfo Holder { get; protected set; }

		public TypeInfo Type { get; protected set; }

		#region [AST]

		public int column { get; set; }

		public int line { get; set; }

		public string Lex { get; set; }

		public bool Pure { get; protected set; }

		#endregion

		public virtual bool BindName(ISemanticChecker sc, ErrorReport report)
		{
			return true;
		}

		public virtual bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{
			if(!this.AutoCheck(sc, report)) return false;

			Type = sc.GetType(HolderType, report, line, column);
			if (Type == null) return false;

			Holder = new HolderInfo
			{
				Name = HolderName,
				Type = Type
			};

			if (sc.DeclareMember(HolderName, new MemberDefinition {line = line, column = column, Member = Holder})) return true;

			report.Add(new StaticError {Line = line, Column = column, ErrorMessage = $"Duplicated argument name {HolderName}", Level = ErrorLevel.Error});

			return false;
		}

		public void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
			where T : class, IType<T, F>
			where F : class, IFunction<T, F>
			where H : class, IHolder
		{
			Holder.BCMMember = cg.GetParam(Position);
		}
	}
}