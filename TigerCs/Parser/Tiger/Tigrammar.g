grammar Tigrammar;

options
{
	language = CSharp3;
}


tokens
{
    //ARITMETIC
	PLUS = '+';
	MINUS = '-';
	MULT = '*';
	DIV = '/';

	//RELATIONAL
	EQUAL = '=';
	NOT_EQUAL = '<>';
	GTHAN = '>';
	LTHAN = '<';
	GTHAN_EQUAL = '>=';
	LTHAN_EQUAL = '<=';

	//LOGICAL
	AND = '&';
	OR = '|';

	//ASSIGNATION
	ASSIGN = ':=';

	//ACCESS
	DOT = '.';	
	L_PARENT = '(';
	R_PARENT = ')';
	L_BRACKETS = '[';
	R_BRACKETS = ']';
	L_KEY ='{';
	R_KEY = '}';	
	
	COLON = ':';
	SEMICOLON =';';
	COMMA = ',';
	DQUOTE='"';

	///key words
	NIL = 'nil';	
	BREAK = 'break';
  	IF='if';
    	THEN='then';
    	ELSE='else';
    	FOR='for';
   	WHILE='while';
    	DO='do';
    	TO='to';
    	LET='let';
    	IN='in';
   	END='end';
    	OF= 'of';
    	ARRAY='array';
    	TYPE='type';
    	VAR='var';
    	FUNCTION='function';
}

@header
{
	using System;
	using TigerCs.Generation.AST.Expressions;
	using TigerCs.Generation.AST.Declarations;
}

@members
{
	enum DclType {None, Type, Var, Function}
}

ID  : LETTER (LETTER|DIGIT|'_')*;

fragment
LETTER	: 	('a'..'z'|'A'..'Z');
	
fragment
DIGIT	:	 ('0'..'9'); 



INT :	('1'..'9') DIGIT*|'0';

COMMENT	:	'/*' ('*' ~('/') | '/' ~('*')	| ~('*'|'/'))* ('*/'|( COMMENT ('*' ~('/') | '/' ~('*')| ~('*'|'/') )* )+'*/')  {$channel=Hidden;};



WS  :   ( ' '
        | '\t'
        | '\r'
        | '\n'
        | '\f'
        ) {$channel=Hidden;}
    ;

STRING
    :  '"' ( ESC_SEQ| ~('\\'|'"') )* '"'
    ;


fragment
ESC_SEQ
    :   '\\' ('t'|'n'|'"'|'\\'|'^'CONTROL_CHARS|DECIMAL_ESC|(WS)+'\\')
    ;

fragment
DECIMAL_ESC
    :    ('0'..'9') ('0'..'9') ('0'..'9')
    |    ('0'..'9') ('0'..'9')
    |    ('0'..'9')
    ;

fragment
CONTROL_CHARS
	:	
	'@'..'_'
	;

public program returns [IExpression r]
: e=expression {r = e;} EOF
;

expression returns [IExpression r]
: o=or_expression {r = o;} (e=or_expression_rest[r] {r = e;})?
;

or_expression returns [IExpression r]
: a=and_expression {r = a;} (e=and_expression_rest[r] {r = e;})?
;

or_expression_rest [IExpression l] returns [IExpression r]
: OR e=expression {r = new IntegerOperator {line = $OR.Line, column = $OR.CharPositionInLine, Left = l, Right = e, Optype = IntegerOp.Or};}
;

and_expression returns [IExpression r]
: a=aritmetic_expression {r = a;} (l=relational_expression[r] {r = l;})?
;

and_expression_rest [IExpression l] returns [IExpression r]
: AND e=or_expression {r = new IntegerOperator {line = $AND.Line, column = $AND.CharPositionInLine, Left = l, Right = e, Optype = IntegerOp.And};}
;

relational_expression [IExpression a] returns [IExpression r]
: EQUAL b=aritmetic_expression {r = new EqualityOperator {line = $EQUAL.Line, column = $EQUAL.CharPositionInLine, Left = a, Right = b};}
| NOT_EQUAL b=aritmetic_expression {r = new EqualityOperator {line = $NOT_EQUAL.Line, column = $NOT_EQUAL.CharPositionInLine, Left = a, Right = b, Equal = false };}
| GTHAN b=aritmetic_expression {r = new GreaterThan {line = $GTHAN.Line, column = $GTHAN.CharPositionInLine, Left = a, Right = b};}
| LTHAN b=aritmetic_expression {r = new LessThan {line = $LTHAN.Line, column = $LTHAN.CharPositionInLine, Left = a, Right = b};}
| GTHAN_EQUAL b=aritmetic_expression {r = new GreaterEqualThan {line = $GTHAN_EQUAL.Line, column = $GTHAN_EQUAL.CharPositionInLine, Left = a, Right = b};}
| LTHAN_EQUAL b=aritmetic_expression {r = new LessEqualThan {line = $LTHAN_EQUAL.Line, column = $LTHAN_EQUAL.CharPositionInLine, Left = a, Right = b};}
;

aritmetic_expression returns[IExpression r]
: t=term {r = t;} (a=aritmetic_expression_rest[r] {r = a;} )?
;

aritmetic_expression_rest [IExpression a] returns [IExpression r]
: op=(MINUS | PLUS)
	t=term {r = new IntegerOperator {line = $op.Line, column = $op.CharPositionInLine, Left = a, Right = t, Optype = new IntegerOp($op.text[0])};} 
	(e=aritmetic_expression_rest[r] {r = e;})?//watch!
;

term returns [IExpression r]
:f=factor {r=f;} (a=term_rest[f] {r=a;})?
;

term_rest [IExpression a] returns [IExpression r]
: op=(MULT|DIV) 
	f=factor {r = new IntegerOperator {line = $op.Line, column = $op.CharPositionInLine, Left = a, Right = f, Optype = new IntegerOp($op.text[0])};}
	(t=term_rest[r] {r = t;})?
;

factor returns [IExpression r]
: s=STRING {r = new StringConstant{Lex = $s.text, line = $s.Line, column = $s.CharPositionInLine};} 
| i=INT {r = new IntegerConstant{Lex = $i.text, line = $i.Line, column = $i.CharPositionInLine};}
| n=NIL {r = new NilConstant{line = $n.Line, column = $n.CharPositionInLine};}
| MINUS e=expression {r = new Neg{line = $MINUS.Line, column = $MINUS.CharPositionInLine, Operand = e};}
| L_PARENT (e=expression_sequence {r = e;})? R_PARENT
| IF c=expression THEN t=expression {r = new IfThenElse {line = $IF.Line, column = $IF.CharPositionInLine, If = c, Then = t};} (ELSE e=expression {((IfThenElse)r).Else = e;})?
| WHILE e1=expression DO e2=expression {r = new While{line = $WHILE.Line, column = $WHILE.CharPositionInLine, Condition = e1, Body = e2};}
| FOR i=ID ASSIGN e1=expression TO e2=expression DO e3=expression {r = new BoundedFor{line = $FOR.Line, column = $FOR.CharPositionInLine, VarName = $i.text, From = e1, To = e2, Body = e3};}
| BREAK {r = new Break{line = $BREAK.Line, column = $BREAK.CharPositionInLine};}
| LET d=declaration_list_list IN e=expression END {r = new Let{line = $LET.Line, column = $LET.CharPositionInLine, Declarations = d, Body = e};}
| h=lvalue_head {r = h;}
;

lvalue_head returns [IExpression r]
@after{r = r ??  new Var {Name = $i.text, line = $i.Line, column = $i.CharPositionInLine};}
: i=ID (ins=invoke[$i.text, $i.Line, $i.CharPositionInLine] {r = ins;} 
		| L_BRACKETS e1=expression R_BRACKETS a=array[$i.text, e1, $i.Line, $i.CharPositionInLine, $L_BRACKETS.Line, $L_BRACKETS.CharPositionInLine] {r=a;}
		| DOT i2=ID l=dot[$i.text, $i2.text, $i.Line, $i.CharPositionInLine, $i2.Line, $i2.CharPositionInLine] {r = l;} )?
;

invoke [string id, int l, int c] returns [IExpression r]
: L_PARENT a=arg_list? R_PARENT {r = new Call{line = l, column = c, Arguments = a ?? new List<IExpression>() , FunctionName = id};}
| L_KEY f=field_list? R_KEY { r = new RecordCreation{line = l, column = c, Members = f ?? new List<Tuple<string, IExpression>>(), Name = id};}
;

array [string id, IExpression e1, int l, int c, int bl, int bc] returns [IExpression r]
: OF e2=expression {r = new ArrayCreation{line = l, column = c, Length = e1, Init = e2, ArrayOf = id};}
| e2=lvalue[new ArrayAccess {Array = new Var {Name = id, line = l, column = c}, Indexer = e1, line = bl, column = bc}] {r = e2;}
;

dot [string idr, string idm, int l, int c, int dl, int dc] returns [IExpression r]
: e2=lvalue [new MemberAccess {MemberName = idm, Record = new Var {Name = idr, line = l, column = c}, line = dl, column = dc}] {r = e2;}
;

lvalue [ILValue var] returns [IExpression r]
@after{ r = r ?? var; }
: (DOT id=ID {var = new MemberAccess {MemberName = $id.text, Record = var, line = $DOT.Line, column = $DOT.CharPositionInLine};} 
	| L_BRACKETS indx=expression R_BRACKETS {var = new ArrayAccess {Array = var, Indexer = indx, line = $L_BRACKETS.Line, column = $L_BRACKETS.CharPositionInLine};} )* 
	(ASSIGN e=expression {r = new Assign {Target = var, Source = e, line = $ASSIGN.Line, column = $ASSIGN.CharPositionInLine};})?
;

arg_list returns [List<IExpression> r]
@init
{
	r =  new List<IExpression>();
}
: e=expression {r.Add(e);} (COMMA e1= expression {r.Add(e1);})*
;

field_list returns[List<Tuple<string, IExpression>> r]
@init
{
	r = new List<Tuple<string, IExpression>>();
}
: i=ID ASSIGN e=expression{r.Add(new Tuple<string, IExpression>($i.text, e));} (COMMA i=ID ASSIGN e=expression {r.Add(new Tuple<string, IExpression>($i.text, e));})*
;

expression_sequence returns[ExpressionList<IExpression> r]
@init
{
	r = new ExpressionList<IExpression>();
}
: e1=expression{r.Add(e1);} ( SEMICOLON e2=expression{r.Add(e1);})*
;

declaration_list_list returns [List<IDeclarationList<IDeclaration>> r]
@init
{
	DclType t = DclType.None;
	r = new List<IDeclarationList<IDeclaration>>();
}
:(e=declaration[r, t] {t = e;})+
;

declaration [List<IDeclarationList<IDeclaration>> r, DclType prec] returns [DclType rt]
@init
{
	TypeDeclaration t = null;
	FunctionDeclaration f = null;
	VarDeclaration v = null;
}
@after
{
	rt = t != null? DclType.Type : f != null? DclType.Function : v != null? DclType.Var : DclType.None;
	
	switch(rt)
	{
		case DclType.Type:
			if(rt == prec) ((TypeDeclarationList)r[r.Count-1]).Add(t);
			else r.Add(new TypeDeclarationList {t});
			break;
		case DclType.Function:
			if(rt == prec) ((FunctionDeclarationList)r[r.Count-1]).Add(f);
			else r.Add(new FunctionDeclarationList {f});
			break;
		case DclType.Var:
			if(rt == prec) ((DeclarationList<VarDeclaration>)r[r.Count-1]).Add(v);
			else r.Add(new DeclarationList<VarDeclaration> {v});
			break;
		default:
			throw new InvalidOperationException();
	} 
}
: e1=function_declaration {f = e1;}
| e2=type_declaration {t = e2;}
| e3=var_declaration {v = e3;}			
;

function_declaration returns [FunctionDeclaration r]
@init{string type = null;}
: FUNCTION i=ID L_PARENT p=type_fields R_PARENT (COLON t=ID {type = $t.text;})? ASSIGN e=expression{ r = new FunctionDeclaration{line = $FUNCTION.Line, column = $FUNCTION.CharPositionInLine, Parameters = p, Return = type, FunctionName = $i.text, Body = e};}
;

var_declaration returns [VarDeclaration r]
: VAR i=ID (ASSIGN e=expression {r = new VarDeclaration {line = $VAR.Line, column = $VAR.CharPositionInLine, HolderName = $i.text, Init = e};}
	| COLON ti=ID ASSIGN e=expression {r = new VarDeclaration {line = $VAR.Line, column = $VAR.CharPositionInLine, HolderName = $i.text, HolderType = $ti.text, Init = e};})
;	
	
type_declaration returns [TypeDeclaration r]
: TYPE i=ID EQUAL t1= type {t1.TypeName = $i.text; t1.line = $TYPE.line; t1.column = $TYPE.CharPositionInLine; r = t1;}
;

type returns [TypeDeclaration r]
: ARRAY OF i=ID {r = new ArrayDeclaration{ArrayOf = $i.text};}
| i=ID {r = new AliasDeclaration{AliasOf = $i.text};}
| l=L_KEY t=type_creation_fields? R_KEY {r = new RecordDeclaration{Members = t};}	
;

type_creation_fields returns[List<Tuple<string, string>> r]
@init
{
	r = new List<Tuple<string, string>>();
}
: i=ID COLON ti=ID {r.Add(new Tuple<string, string>($i.text,$ti.text));} (i=ID COLON ti=ID {r.Add(new Tuple<string, string>($i.text,$ti.text));})*
;

type_fields returns[List<ParameterDeclaration> r]
@init
{
	r = new List<ParameterDeclaration>();
	int c = 0;
}
: (t=type_field{t.Position = c; r.Add(t); c++;})*
;

type_field returns[ParameterDeclaration r]
: i=ID COLON t=ID {r = new ParameterDeclaration{line = $i.Line, column = $i.CharPositionInLine, HolderName = $i.text, HolderType = $t.text};}
;
