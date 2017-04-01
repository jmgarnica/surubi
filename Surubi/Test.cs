using TigerCs.CompilationServices;
using TigerCs.Emitters.NASM;
using TigerCs.Emitters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TigerCs.Generation.AST.Declarations;
using TigerCs.Generation.AST.Expressions;

namespace Surubi
{

	class Test
	{
		static void Main(string[] args)
		{
			var r = new ErrorReport();
			NasmEmitter e = new NasmEmitter("ex");
			DefaultSemanticChecker dsc = new DefaultSemanticChecker();

			#region AST

			Generator<NasmType, NasmFunction, NasmHolder> tg = new Generator<NasmType, NasmFunction, NasmHolder>
			{
				SemanticChecker = dsc,
				ByteCodeMachine = e,
				Parser = new TigerCs.Parser.Tiger.Parser()
			};


			#region [AutoCheck]

			//var m = new IfThenElse
			//{
			//	If = new IntegerConstant {Lex = "0"},
			//	Else = new IntegerConstant { Lex = ""},
			//	Then = new IntegerConstant ()
			//};

			#endregion

			//var m = tg.Parse(new StringReader("(printi(argc); for i:=0 to argc-1 do prints(args[i]))"), r);

			string equ = "let /* The eight queens solver from Appel */ \n" +
			             "var N := 8 \n" +
			             "type intArray = array of int \n" +
			             "var row:= intArray[N] of 0 \n" +
			             "var col := intArray[N] of 0 \n" +
			             "var diag1 := intArray[N + N - 1] of 0 \n" +
			             "var diag2 := intArray[N + N - 1] of 0 \n" +
			             "function printboard() := \n" +
			             "(for i := 0 to N - 1 \n" +
			             "do (for j := 0 to N - 1 \n" +
			             " do prints(if col[i] = j then "+'"'+ " O"+'"'+" else "+'"'+" ."+'"'+"); \n" +
			             "prints(\"\n\")); \n" +
			             "prints(\"\n\"))\n" +
			             "function try(c:int) := \n" +
			             "if c = N then printboard() \n" +
			             "else for r := 0 to N - 1 \n" +
			             "do if row[r] = 0 & \n" +
			             "diag1[r + c] = 0 & diag2[r + 7 - c] = 0\n" +
			             "then(row[r] := 1; diag1[r + c] := 1; \n" +
			             " diag2[r + 7 - c] := 1; col[c] := r; \n" +
			             " try(c + 1);\n" +
			             " row[r] := 0; diag1[r + c] := 0; \n" +
						 "diag2[r + 7 - c] := 0) \n" +
			             "in try(0) end \n";
			var m = tg.Parse(new StringReader(equ), r);

				if(m != null)
				tg.Compile(m, r);

			int count = r.Count();
			Console.WriteLine("Compilation " + (count == 0
													? "success"
													: $"fail with {count} error{(count > 1 ? "s" : "")}:"));

			Console.WriteLine();

			foreach (var error in r)
				Console.WriteLine(error);

			if (Debugger.IsAttached) Console.ReadKey();

			#endregion
		}
	}
}


#region [eight queens solver from Appel]

//var m = new Let
//{
//	Declarations = new List<IDeclarationList<IDeclaration>>
//				{
//					new DeclarationList<VarDeclaration>
//					{
//						new VarDeclaration
//						{
//							HolderName = "N",
//							Init = new IntegerConstant {Lex = "8"},
//							line = 0
//						},
//						new VarDeclaration
//						{
//							HolderName = "C",
//							Init = new IntegerConstant {Lex = "0"},
//							line = 0
//						}
//					},
//					new TypeDeclarationList
//					{
//						new ArrayDeclaration
//						{
//							ArrayOf = "int",
//							TypeName = "intArray",
//							line = 1
//						}
//					},

//					#region [Board Vars]

//					new DeclarationList<VarDeclaration>
//					{
//						new VarDeclaration
//						{
//							HolderName = "row",
//							Init = new ArrayCreation
//							{
//								ArrayOf = "intArray",
//								Length = new Var {Name = "N"},
//								Init = new IntegerConstant {Lex = "0"},
//							},
//							line = 2
//						},
//						new VarDeclaration
//						{
//							HolderName = "col",
//							Init = new ArrayCreation
//							{
//								ArrayOf = "intArray",
//								Length = new Var {Name = "N"},
//								Init = new IntegerConstant {Lex = "0"},
//							},
//							line = 3
//						},
//						new VarDeclaration
//						{
//							HolderName = "diag1",
//							Init = new ArrayCreation
//							{
//								ArrayOf = "intArray",
//								Length = new IntegerOperator
//								{
//									Left = new Var {Name = "N"},
//									Right = new IntegerOperator
//									{
//										Left = new Var {Name = "N"},
//										Right = new IntegerConstant {Lex = "1"},
//										Optype = IntegerOp.Subtraction
//									},
//									Optype = IntegerOp.Addition
//								},
//								Init = new IntegerConstant {Lex = "0"},
//							},
//							line = 4
//						},
//						new VarDeclaration
//						{
//							HolderName = "diag2",
//							Init = new ArrayCreation
//							{
//								ArrayOf = "intArray",
//								Length = new IntegerOperator
//								{
//									Left = new Var {Name = "N"},
//									Right = new IntegerOperator
//									{
//										Left = new Var {Name = "N"},
//										Right = new IntegerConstant {Lex = "1"},
//										Optype = IntegerOp.Subtraction
//									},
//									Optype = IntegerOp.Addition
//								},
//								Init = new IntegerConstant {Lex = "0"},
//							},
//							line = 5
//						}
//					},

//					#endregion

//					new FunctionDeclarationList
//					{
//						#region [PrintBoard]
//						new FunctionDeclaration
//						{
//							FunctionName = "printboard",
//							Parameters = new List<ParameterDeclaration>(),
//							Body = new ExpressionList<IExpression>
//							{
//								new Assign
//								{
//									Source = new IntegerOperator
//									{
//										Left = new Var {Name = "C"},
//										Right = new IntegerConstant {Lex = "1"},
//										Optype = IntegerOp.Addition
//									},
//									Target = new Var {Name = "C"}
//								},
//								new BoundedFor
//								{
//									VarName = "i",
//									From = new IntegerConstant {Lex = "0"},
//									To = new IntegerOperator
//									{
//										Left = new Var {Name = "N"},
//										Right = new IntegerConstant {Lex = "1"},
//										Optype = IntegerOp.Subtraction
//									},
//									Body = new ExpressionList<IExpression>
//									{
//										new BoundedFor
//										{
//											VarName = "j",
//											From = new IntegerConstant {Lex = "0"},
//											To = new IntegerOperator
//											{
//												Left = new Var {Name = "N"},
//												Right = new IntegerConstant {Lex = "1"},
//												Optype = IntegerOp.Subtraction
//											},
//											Body = new Call
//											{
//												FunctionName = "prints",
//												Arguments = new ExpressionList<IExpression>
//												{
//													new IfThenElse
//													{
//														If = new EqualityOperator
//														{
//															Left = new ArrayAccess
//															{
//																Array = new Var {Name = "col"},
//																Indexer = new Var {Name = "i"}
//															},
//															Right = new Var {Name = "j"}
//														},
//														Then = new StringConstant {Lex = " 0"},
//														Else = new StringConstant {Lex = " ."}

//													}
//												}
//											}
//										},
//										new Call
//										{
//											FunctionName = "prints",
//											Arguments = new ExpressionList<IExpression>
//											{
//												new StringConstant {Lex = "\n"}
//											}
//										}
//									}
//								},
//								new Call
//								{
//									FunctionName = "prints",
//									Arguments = new ExpressionList<IExpression>
//									{
//										new StringConstant {Lex = "\n"}
//									}
//								}
//							}
//						},

//						#endregion

//						#region [Try]
//						new FunctionDeclaration
//						{
//							FunctionName = "try",
//							Parameters = new List<ParameterDeclaration>
//							{
//								new ParameterDeclaration
//								{
//									HolderName = "c",
//									HolderType = "int",
//									Position = 0 //no en la gramatica
//								}
//							},
//							Body = new IfThenElse
//							{
//								If = new EqualityOperator
//								{
//									Left = new Var {Name = "c"},
//									Right = new Var {Name = "N"}
//								},
//								Then = new Call
//								{
//									FunctionName = "printboard",
//									Arguments = new ExpressionList<IExpression>()
//								},
//								Else = new BoundedFor
//								{
//									VarName = "r",
//									From = new IntegerConstant {Lex = "0"},
//									To = new IntegerOperator
//									{
//										Left = new Var {Name = "N"},
//										Right = new IntegerConstant {Lex = "1"},
//										Optype = IntegerOp.Subtraction
//									},
//									Body = new IfThenElse
//									{
//										#region [IF]
//										If = new IntegerOperator
//										{
//											Left = new EqualityOperator
//											{
//												Left = new ArrayAccess
//												{
//													Array = new Var {Name = "row"},
//													Indexer = new Var {Name = "r"}
//												},
//												Right = new IntegerConstant {Lex = "0"}
//											},
//											Right = new IntegerOperator
//											{
//												Left = new EqualityOperator
//												{
//													Left = new ArrayAccess
//													{
//														Array = new Var {Name = "diag1"},
//														Indexer = new IntegerOperator
//														{
//															Left = new Var {Name = "r"},
//															Right = new Var {Name = "c"},
//															Optype = IntegerOp.Addition
//														}
//													},
//													Right = new IntegerConstant {Lex = "0"}
//												},
//												Right = new EqualityOperator
//												{
//													Left = new ArrayAccess
//													{
//														Array = new Var {Name = "diag2"},
//														Indexer = new IntegerOperator
//														{
//															Left = new Var {Name = "r"},
//															Right = new IntegerOperator
//															{
//																Left = new IntegerConstant {Lex = "7"},
//																Right = new Var {Name = "c"},
//																Optype = IntegerOp.Subtraction
//															},
//															Optype = IntegerOp.Addition
//														}
//													},
//													Right = new IntegerConstant {Lex = "0"}
//												},
//												Optype = IntegerOp.And
//											},
//											Optype = IntegerOp.And
//										},

//										#endregion

//										#region [Then]

//										Then = new ExpressionList<IExpression>
//										{
//											new Assign
//											{
//												Source = new IntegerConstant {Lex = "1"},
//												Target = new ArrayAccess
//												{
//													Array = new Var {Name = "row"},
//													Indexer = new Var {Name = "r"}
//												}
//											},
//											new Assign
//											{
//												Source = new IntegerConstant {Lex = "1"},
//												Target = new ArrayAccess
//												{
//													Array = new Var {Name = "diag1"},
//													Indexer = new IntegerOperator
//													{
//														Left = new Var {Name = "r"},
//														Right = new Var {Name = "c"},
//														Optype = IntegerOp.Addition
//													}
//												}
//											},
//											new Assign
//											{
//												Source = new IntegerConstant {Lex = "1"},
//												Target = new ArrayAccess
//												{
//													Array = new Var {Name = "diag2"},
//													Indexer = new IntegerOperator
//													{
//														Left = new Var {Name = "r"},
//														Right = new IntegerOperator
//														{
//															Left = new IntegerConstant {Lex = "7"},
//															Right = new Var {Name = "c"},
//															Optype = IntegerOp.Subtraction
//														},
//														Optype = IntegerOp.Addition
//													}
//												}
//											},
//											new Assign
//											{
//												Source = new Var {Name = "r"},
//												Target = new ArrayAccess
//												{
//													Array = new Var {Name = "col"},
//													Indexer = new Var {Name = "c"}
//												}
//											},
//											new Call
//											{
//												FunctionName = "try",
//												Arguments = new ExpressionList<IExpression>
//												{
//													new IntegerOperator
//													{
//														Left = new Var {Name = "c"},
//														Right = new IntegerConstant {Lex = "1"},
//														Optype = IntegerOp.Addition
//													}
//												}
//											},
//											new Assign
//											{
//												Source = new IntegerConstant {Lex = "0"},
//												Target = new ArrayAccess
//												{
//													Array = new Var {Name = "row"},
//													Indexer = new Var {Name = "r"}
//												}
//											},
//											new Assign
//											{
//												Source = new IntegerConstant {Lex = "0"},
//												Target = new ArrayAccess
//												{
//													Array = new Var {Name = "diag1"},
//													Indexer = new IntegerOperator
//													{
//														Left = new Var {Name = "r"},
//														Right = new Var {Name = "c"},
//														Optype = IntegerOp.Addition
//													}
//												}
//											},
//											new Assign
//											{
//												Source = new IntegerConstant {Lex = "0"},
//												Target = new ArrayAccess
//												{
//													Array = new Var {Name = "diag2"},
//													Indexer = new IntegerOperator
//													{
//														Left = new Var {Name = "r"},
//														Right = new IntegerOperator
//														{
//															Left = new IntegerConstant {Lex = "7"},
//															Right = new Var {Name = "c"},
//															Optype = IntegerOp.Subtraction
//														},
//														Optype = IntegerOp.Addition
//													}
//												}
//											}
//										}
//										#endregion

//									}
//								}
//							}
//						}
//						#endregion
//					}
//				},
//	Body = new ExpressionList<IExpression>
//				{
//					new Call
//					{
//						Arguments = new ExpressionList<IExpression>
//						{
//							new IntegerConstant {Lex = "0"}
//						},
//						FunctionName = "try"
//					},
//					new Call
//					{
//						Arguments = new ExpressionList<IExpression>
//						{
//							new Var { Name = "C"}
//						},
//						FunctionName = "printi"
//					}
//				}
//};

#endregion