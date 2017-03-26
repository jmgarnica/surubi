using TigerCs.CompilationServices;
using TigerCs.Emitters.NASM;
using TigerCs.Emitters;
using System;
using System.Diagnostics;
using TigerCs.Generation.AST.Declarations;
using TigerCs.Generation.AST.Expressions;

namespace Surubi
{

	class Test
	{
		static void Main(string[] args)
		{
			var r = new ErrorReport();
			NasmEmitter e = new NasmEmitter("ex.asm");
			DefaultSemanticChecker dsc = new DefaultSemanticChecker();

			#region AST

			TigerGenerator<NasmType, NasmFunction, NasmHolder> tg = new TigerGenerator<NasmType, NasmFunction, NasmHolder>
			{
				SemanticChecker = dsc,
				ByteCodeMachine = e
			};

			#region [eight queens solver from Appel]

			var m = new Let
			{
				Declarations = new DeclarationListList<IDeclaration>
				{
					new DeclarationList<VarDeclaration>
					{
						new VarDeclaration
						{
							HolderName = "N",
							Init = new IntegerConstant {Lex = "8"},
							line = 0
						},
						new VarDeclaration
						{
							HolderName = "C",
							Init = new IntegerConstant {Lex = "0"},
							line = 0
						}
					},
					new TypeDeclarationList
					{
						new ArrayDeclaration
						{
							ArrayOf = "int",
							TypeName = "intArray",
							line = 1
						}
					},

					#region [Board Vars]

					new DeclarationList<VarDeclaration>
					{
						new VarDeclaration
						{
							HolderName = "row",
							Init = new ArrayCreation
							{
								ArrayOf = "intArray",
								Length = new Var {Name = "N"},
								Init = new IntegerConstant {Lex = "0"},
							},
							line = 2
						},
						new VarDeclaration
						{
							HolderName = "col",
							Init = new ArrayCreation
							{
								ArrayOf = "intArray",
								Length = new Var {Name = "N"},
								Init = new IntegerConstant {Lex = "0"},
							},
							line = 3
						},
						new VarDeclaration
						{
							HolderName = "diag1",
							Init = new ArrayCreation
							{
								ArrayOf = "intArray",
								Length = new IntegerOperator
								{
									Left = new Var {Name = "N"},
									Rigth = new IntegerOperator
									{
										Left = new Var {Name = "N"},
										Rigth = new IntegerConstant {Lex = "1"},
										Optype = IntegerOp.Subtraction
									},
									Optype = IntegerOp.Addition
								},
								Init = new IntegerConstant {Lex = "0"},
							},
							line = 4
						},
						new VarDeclaration
						{
							HolderName = "diag2",
							Init = new ArrayCreation
							{
								ArrayOf = "intArray",
								Length = new IntegerOperator
								{
									Left = new Var {Name = "N"},
									Rigth = new IntegerOperator
									{
										Left = new Var {Name = "N"},
										Rigth = new IntegerConstant {Lex = "1"},
										Optype = IntegerOp.Subtraction
									},
									Optype = IntegerOp.Addition
								},
								Init = new IntegerConstant {Lex = "0"},
							},
							line = 5
						}
					},

					#endregion

					new FunctionDeclarationList
					{
						#region [PrintBoard]
						new FunctionDeclaration
						{
							FunctionName = "printboard",
							Parameters = new DeclarationList<ParameterDeclaration>(),
							Body = new ExpressionList<IExpression>
							{
								new Assign
								{
									Source = new IntegerOperator
									{
										Left = new Var {Name = "C"},
										Rigth = new IntegerConstant {Lex = "1"},
										Optype = IntegerOp.Addition
									},
									Target = new Var {Name = "C"}
								},
								new BoundedFor
								{
									VarName = "i",
									From = new IntegerConstant {Lex = "0"},
									To = new IntegerOperator
									{
										Left = new Var {Name = "N"},
										Rigth = new IntegerConstant {Lex = "1"},
										Optype = IntegerOp.Subtraction
									},
									Body = new ExpressionList<IExpression>
									{
										new BoundedFor
										{
											VarName = "j",
											From = new IntegerConstant {Lex = "0"},
											To = new IntegerOperator
											{
												Left = new Var {Name = "N"},
												Rigth = new IntegerConstant {Lex = "1"},
												Optype = IntegerOp.Subtraction
											},
											Body = new Call
											{
												FunctionName = "prints",
												Arguments = new ExpressionList<IExpression>
												{
													new IfThenElse
													{
														If = new EqualityOperator
														{
															Left = new ArrayAccess
															{
																Array = new Var {Name = "col"},
																Indexer = new Var {Name = "i"}
															},
															Rigth = new Var {Name = "j"}
														},
														Then = new StringConstant {Lex = " 0"},
														Else = new StringConstant {Lex = " ."}

													}
												}
											}
										},
										new Call
										{
											FunctionName = "prints",
											Arguments = new ExpressionList<IExpression>
											{
												new StringConstant {Lex = "\n"}
											}
										}
									}
								},
								new Call
								{
									FunctionName = "prints",
									Arguments = new ExpressionList<IExpression>
									{
										new StringConstant {Lex = "\n"}
									}
								}
							}
						},

						#endregion

						#region [Try]
						new FunctionDeclaration
						{
							FunctionName = "try",
							Parameters = new DeclarationList<ParameterDeclaration>
							{
								new ParameterDeclaration
								{
									HolderName = "c",
									HolderType = "int",
									Position = 0 //no en la gramatica
								}
							},
							Body = new IfThenElse
							{
								If = new EqualityOperator
								{
									Left = new Var {Name = "c"},
									Rigth = new Var {Name = "N"}
								},
								Then = new Call
								{
									FunctionName = "printboard",
									Arguments = new ExpressionList<IExpression>()
								},
								Else = new BoundedFor
								{
									VarName = "r",
									From = new IntegerConstant {Lex = "0"},
									To = new IntegerOperator
									{
										Left = new Var {Name = "N"},
										Rigth = new IntegerConstant {Lex = "1"},
										Optype = IntegerOp.Subtraction
									},
									Body = new IfThenElse
									{
										#region [IF]
										If = new IntegerOperator
										{
											Left = new EqualityOperator
											{
												Left = new ArrayAccess
												{
													Array = new Var {Name = "row"},
													Indexer = new Var {Name = "r"}
												},
												Rigth = new IntegerConstant {Lex = "0"}
											},
											Rigth = new IntegerOperator
											{
												Left = new EqualityOperator
												{
													Left = new ArrayAccess
													{
														Array = new Var {Name = "diag1"},
														Indexer = new IntegerOperator
														{
															Left = new Var {Name = "r"},
															Rigth = new Var {Name = "c"},
															Optype = IntegerOp.Addition
														}
													},
													Rigth = new IntegerConstant {Lex = "0"}
												},
												Rigth = new EqualityOperator
												{
													Left = new ArrayAccess
													{
														Array = new Var {Name = "diag2"},
														Indexer = new IntegerOperator
														{
															Left = new Var {Name = "r"},
															Rigth = new IntegerOperator
															{
																Left = new IntegerConstant {Lex = "7"},
																Rigth = new Var {Name = "c"},
																Optype = IntegerOp.Subtraction
															},
															Optype = IntegerOp.Addition
														}
													},
													Rigth = new IntegerConstant {Lex = "0"}
												},
												Optype = IntegerOp.And
											},
											Optype = IntegerOp.And
										},

										#endregion

										#region [Then]

										Then = new ExpressionList<IExpression>
										{
											new Assign
											{
												Source = new IntegerConstant {Lex = "1"},
												Target = new ArrayAccess
												{
													Array = new Var {Name = "row"},
													Indexer = new Var {Name = "r"}
												}
											},
											new Assign
											{
												Source = new IntegerConstant {Lex = "1"},
												Target = new ArrayAccess
												{
													Array = new Var {Name = "diag1"},
													Indexer = new IntegerOperator
													{
														Left = new Var {Name = "r"},
														Rigth = new Var {Name = "c"},
														Optype = IntegerOp.Addition
													}
												}
											},
											new Assign
											{
												Source = new IntegerConstant {Lex = "1"},
												Target = new ArrayAccess
												{
													Array = new Var {Name = "diag2"},
													Indexer = new IntegerOperator
													{
														Left = new Var {Name = "r"},
														Rigth = new IntegerOperator
														{
															Left = new IntegerConstant {Lex = "7"},
															Rigth = new Var {Name = "c"},
															Optype = IntegerOp.Subtraction
														},
														Optype = IntegerOp.Addition
													}
												}
											},
											new Assign
											{
												Source = new Var {Name = "r"},
												Target = new ArrayAccess
												{
													Array = new Var {Name = "col"},
													Indexer = new Var {Name = "c"}
												}
											},
											new Call
											{
												FunctionName = "try",
												Arguments = new ExpressionList<IExpression>
												{
													new IntegerOperator
													{
														Left = new Var {Name = "c"},
														Rigth = new IntegerConstant {Lex = "1"},
														Optype = IntegerOp.Addition
													}
												}
											},
											new Assign
											{
												Source = new IntegerConstant {Lex = "0"},
												Target = new ArrayAccess
												{
													Array = new Var {Name = "row"},
													Indexer = new Var {Name = "r"}
												}
											},
											new Assign
											{
												Source = new IntegerConstant {Lex = "0"},
												Target = new ArrayAccess
												{
													Array = new Var {Name = "diag1"},
													Indexer = new IntegerOperator
													{
														Left = new Var {Name = "r"},
														Rigth = new Var {Name = "c"},
														Optype = IntegerOp.Addition
													}
												}
											},
											new Assign
											{
												Source = new IntegerConstant {Lex = "0"},
												Target = new ArrayAccess
												{
													Array = new Var {Name = "diag2"},
													Indexer = new IntegerOperator
													{
														Left = new Var {Name = "r"},
														Rigth = new IntegerOperator
														{
															Left = new IntegerConstant {Lex = "7"},
															Rigth = new Var {Name = "c"},
															Optype = IntegerOp.Subtraction
														},
														Optype = IntegerOp.Addition
													}
												}
											}
										}
										#endregion

									}
								}
							}
						}
						#endregion
					}
				},
				Body = new ExpressionList<IExpression>
				{
					new Call
					{
						Arguments = new ExpressionList<IExpression>
						{
							new IntegerConstant {Lex = "0"}
						},
						FunctionName = "try"
					},
					new Call
					{
						Arguments = new ExpressionList<IExpression>
						{
							new Var { Name = "C"}
						},
						FunctionName = "printi"
					}
				}
			};

			#endregion

			#region [AutoCheck]

			//var m = new IfThenElse
			//{
			//	If = new IntegerConstant {Lex = "0"},
			//	Else = new IntegerConstant { Lex = ""},
			//	Then = new IntegerConstant ()
			//};

			#endregion

			////tg.Compile(m);

			////int count = tg.Report.Count();
			////Console.WriteLine("Compilation " + (count == 0
			////										? "success"
			////										: $"fail with {count} error{(count > 1 ? "s" : "")}:"));

			////Console.WriteLine();

			////foreach (var error in tg.Report)
			////	Console.WriteLine(error);

			if (Debugger.IsAttached) Console.ReadKey();

			#endregion
		}
	}
}
