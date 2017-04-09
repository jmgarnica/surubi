using System.Linq;
using TigerCs.CompilationServices;
using TigerCs.CompilationServices.AutoCheck;
using TigerCs.Generation.ByteCode;
using TigerCs.Interpretation;

namespace TigerCs.Generation.AST.Expressions
{
	public class MemberAccess : Expression, ILValue
	{
		[NotNull]
		public IExpression Record { get; set; }
		public string MemberName { get; set; }

		int index = -1;

		public override bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{
			if (Record == null || string.IsNullOrEmpty(MemberName))
			{
				report.IncompleteMemberInitialization(GetType().Name, line, column);
				return false;
			}

			if (!Record.CheckSemantics(sc, report)) return false;

			if (Record.Return.Members == null)
			{
				report.Add(new StaticError(line, column, $"Type {Record.Return} is not a record type", ErrorLevel.Error));
				return false;
			}

			var member = (from i in Enumerable.Range(0, Record.Return.Members.Count)
						  let c = new { t = Record.Return.Members[i], i }
						  where c.t.Item1 == MemberName
						  select new { t = c.t.Item2, c.i }).FirstOrDefault();

			if (member == null)
			{
				report.Add(new StaticError(line,
				                           column, $"Type {Record.Return.Name} does not have a definition for member {MemberName}",
				                           ErrorLevel.Error));
				return false;
			}

			Return = member.t;
			index = member.i;
			ReturnValue = new HolderInfo { Type = Return, Name = "Member Acces" };

			Pure = Record.Pure;

			IntpObject o = null;
			if (Record.ReturnValue.ConstValue?.TryGetMember(index, out o) == true) ReturnValue.ConstValue = o;

			return true;
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			H member = null;
			if(!Record.Pure) Record.GenerateCode(cg, report);
				if (ReturnValue.ConstValue?.GenerateBCMMember(cg, out member) != true)
			{
				if (Record.Pure) Record.GenerateCode(cg, report);
				ReturnValue.BCMMember = cg.StaticMemberAcces((T)Record.Return.BCMMember, (H)Record.ReturnValue.BCMMember, index);
			}
			else
			{
				ReturnValue.BCMMember = member;
			}
		}

		public void SetValue<T, F, H>(IByteCodeMachine<T, F, H> cg, H source, ErrorReport report)
			where T : class, IType<T, F>
			where F : class, IFunction<T, F>
			where H : class, IHolder
		{
			GenerateCode(cg, report);
			cg.InstrAssing((H)ReturnValue.BCMMember, source);
		}
	}
}