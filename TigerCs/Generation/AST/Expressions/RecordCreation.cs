using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TigerCs.CompilationServices;
using TigerCs.CompilationServices.AutoCheck;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Expressions
{
    public class RecordCreation:Expression
    {
		[NotNull]
        public List<Tuple<string, IExpression>> Members { get; set; }

        [NotNull("")]
        public string Name { get; set; }

        public override bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
        {
            if(!this.AutoCheck(sc, report, null))
            {
                return false;
            }
            var record_type = sc.GetType(Name, report, line, column, false);
            if(record_type == null)
            {
                return false;
            }
            if(record_type.Members == null)
            {
                report.Add(new StaticError(line, column, $"Type {record_type} is not a record type", ErrorLevel.Error));
                return false;
            }

            var cc = Math.Min(record_type.Members.Count, Members.Count);

            for (int i = 0; i < cc; i++)
            {
                var checkresult = Members[i].Item2.CheckSemantics(sc, report, record_type.Members[i].Item2);
                if(Members[i].Item1 != record_type.Members[i].Item1)
                {
                    report.Add(new StaticError(line, column, $"Member {record_type.Members[i].Item1} expected", ErrorLevel.Error));
                    continue;
                }
                if (checkresult)
                {
                    if (Members[i].Item2.Return != record_type.Members[i].Item2)
                        report.Add(new StaticError(line, column, $"Expression [{Members[i].Item1}] must be of type [{record_type.Members[i].Item1}]",ErrorLevel.Error));
                }
            }

            for (int i = cc; i < Members.Count; i++)
            {
                Members[i].Item2.CheckSemantics(sc, report);
                report.Add(new StaticError(line, column, $"Member {Members[i].Item1} not declared in {record_type}", ErrorLevel.Error));
            }
            for (int i = cc; i < record_type.Members.Count; i++)
            {
                report.Add(new StaticError(line, column, $"Member {record_type.Members[i].Item1} expected", ErrorLevel.Error));
            }


            Return = record_type;
            ReturnValue = new HolderInfo { Type = record_type};
            Pure = false;
            CanBreak = false; //expression that returns can't break

            return true;
        }

        public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
        {
            T type = (T)Return.BCMMember;
            H ret = cg.BindVar(type);

			H[] args = new H[Members.Count];
	        for (int i = 0; i < Members.Count; i++)
	        {
		        var member = Members[i];
		        member.Item2.GenerateCode(cg, report);
		        args[i] = (H)member.Item2.ReturnValue.BCMMember;
	        }

	        cg.Call(type.Allocator, args, ret);

            ReturnValue.BCMMember = ret;
        }
    }
}