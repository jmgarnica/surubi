using TigerCs.CompilationServices;
using TigerCs.CompilationServices.AutoCheck;
using TigerCs.Generation.ByteCode;
using System.Text;
using System.Collections.Generic;

namespace TigerCs.Generation.AST.Expressions
{
	public class IntegerConstant : Expression
	{
		[NotNull("")]
		public override string Lex { get; set; }

		public IntegerConstant()
		{
			Pure = true;
		}
		int value;

		public override bool CheckSemantics(ISemanticChecker sp, ErrorReport report, TypeInfo expected = null)
		{
			if (this.AutoCheck(sp, report, expected) && !int.TryParse(Lex, out value))
			{
				report.Add(new StaticError(line, column, "Integer parsing error", ErrorLevel.Internal, Lex));
				//TODO: there is no need ¿for/of? stopping the semantic check, but the checking must fail at the end
			}

			Return = sp.Int(report);
			ReturnValue = new HolderInfo {Type = Return, ConstValue = value};
			return true;
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			ReturnValue.BCMMember = cg.AddConstant(value);
		}
	}

	public class StringConstant : Expression
	{
		[NotNull]
		public override string Lex { get; set; }

		public StringConstant()
		{
			Pure = true;
		}

        public override bool CheckSemantics(ISemanticChecker sp, ErrorReport report, TypeInfo expected = null)
        {
            this.AutoCheck(sp, report, expected);
            //Lex = Lex.Trim('"');
            Return = sp.String(report);

            StringBuilder s = new StringBuilder();
            bool open_scape = false;
            for (int i = 0; i < Lex.Length; i++)
            {
                if ((i == 0 || i == Lex.Length - 1) && Lex[i] == '\"')
                    continue;

                if (Lex[i] < 32 || Lex[i] > 126)
                {
                    report.Add(new StaticError(line, column + i, $"{Lex[i]} invalid character in string literal", ErrorLevel.Error));
                    continue;
                }
                if (Lex[i] == '\\')
                {
                    if (open_scape)
                    {
                        open_scape = false;
                        continue;
                    }
                    #region closed_scape
                    else
                    {
                        if (i + 1 < Lex.Length)
                        {
                            char x = Lex[i + 1];
                            if (x == 'n')
                            {
                                s.Append((char)10);
                            }
                            else if (x == 'r')
                            {
                                s.Append((char)13);
                            }
                            else if (x == 't')
                            {
                                s.Append((char)9);
                            }
                            else if (x == '"')
                            {
                                s.Append((char)34);
                            }
                            else if (x == '\\')
                            {
                                s.Append((char)92);
                            }
                            else if (x >= '0' && x <= '9')
                            {
                                string str = "";
                                str += x;
                                if (i + 2 < Lex.Length)
                                {
                                    var y = Lex[i + 2];
                                    if (y >= 48 && y <= 57)
                                    {
                                        str += y;
                                        if (i + 3 < Lex.Length)
                                        {
                                            y = Lex[i + 3];
                                            if (y >= 48 && y <= 57)
                                            {
                                                str += y;
                                            }
                                            i++;
                                        }
                                    }
                                    i++;
                                }
                                s.Append((char)int.Parse(str));
                            }
                            i++;
                            continue;
                        }
                        else
                        {
                            report.Add(new StaticError(line, column + i, $"{Lex[i]} invalid character in string literal", ErrorLevel.Error));
                        }
                    }
                    #endregion
                    open_scape = true;
                    continue;
                }
                else
                {
                    if (!open_scape)
                    {
                        s.Append(Lex[i]);
                    }
                }
            }
            if(open_scape)
                report.Add(new StaticError(line, column + Lex.Length, $"unexpected end in string literal", ErrorLevel.Error));
            Lex = s.ToString();
            ReturnValue = new HolderInfo { Type = Return };
            return true;
        }

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			ReturnValue.BCMMember = cg.AddConstant(Lex);
		}
	}

	public class NilConstant : Expression
	{
		public NilConstant()
		{
			Pure = true;
		}

		public override bool CheckSemantics(ISemanticChecker sp, ErrorReport report, TypeInfo expected = null)
		{
			Return = sp.Null(report);
			ReturnValue = sp.Nil(report);
			return true;
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			if(ReturnValue.Bounded) return;

			H nil;
			if (!cg.TryBindSTDConst("nil", out nil))
			{
				report.Add(new StaticError(line,column,$"There is no definition for nil in {cg.GetType().FullName}", ErrorLevel.Internal));
				return;
			}

			ReturnValue.BCMMember = nil;
		}
	}
}