using System.Linq;
using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Expresions
{
	public class MemberAccess : Expresion, ILValue
	{
		public IExpresion Record { get; set; }
		public string MemberName { get; set; }

		int index = -1;

		public override bool CheckSemantics(ISemanticChecker sc, ErrorReport report)
		{
			if (Record == null || string.IsNullOrEmpty(MemberName))
			{
				report.IncompleteMemberInitialization(GetType().Name);
				return false;
			}

			if (!Record.CheckSemantics(sc, report)) return false;

			var member = (from i in Enumerable.Range(0, Record.Return.Members.Count)
						  let c = new { t = Record.Return.Members[i], i }
						  where c.t.Item1 == MemberName
						  select new { t = c.t.Item2, c.i }).FirstOrDefault();

			if (member == null)
			{
				report.Add(new StaticError(line,
					column,
				                                $"Type {Record.Return.Name} does not have a definition for member {MemberName}", ErrorLevel.Error));
				return false;
			}

			Return = member.t;
			index = member.i;
			ReturnValue = new HolderInfo { Type = Return, Name = "Member Acces" };
			return true;
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			Record.GenerateCode(cg, report);
			ReturnValue.BCMMember = cg.StaticMemberAcces((T)Return.BCMMember, (H)Record.Return.BCMMember, index);
		}

		public void SetValue<T, F, H>(IByteCodeMachine<T, F, H> cg, H source, ErrorReport report)
			where T : class, IType<T, F>
			where F : class, IFunction<T, F>
			where H : class, IHolder
		{
			GenerateCode(cg, report);
			cg.InstrAssing((H)ReturnValue.BCMMember, source);
			//TODO: probar si no hace nada extraño
		}
	}
}