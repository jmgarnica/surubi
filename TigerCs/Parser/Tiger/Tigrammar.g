grammar Tigrammar;
options{language = CSharp3;}
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

ID  : LETTER (LETTER|DIGIT|'_')*;

fragment
LETTER	: 	('a'..'z'|'A'..'Z');
	
fragment
DIGIT	:	 ('0'..'9'); 



INT :	('1'..'9') DIGIT*|'0';

COMMENT	:	'/*' ('*' ~('/') | '/' ~('*')	| ~('*'|'/'))* ('*/'|( COMMENT ('*' ~('/') | '/' ~('*')| ~('*'|'/') )* )+'*/')  {$channel=HIDDEN;};



WS  :   ( ' '
        | '\t'
        | '\r'
        | '\n'
        | '\f'
        ) {$channel=HIDDEN;}
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
	'@'..'_';



program returns[IExpression r]
: e=expression {r = e} EOF;

expression returns [IExpression r]
: s=STRING {r = new StringConst{Lex = $s.text, line = $s.Line, column = $s.CharPositionInLine};}
| i=INT {r = new IntConst{Lex = i.text, line = $i.Line, column = $i.CharPositionInLine};}
| n=NIL {r = new NilConst{line = $n.Line, column = $n.CharPositionInLine};}
| l=lvalue {r = l;}
| m=MINUS e=expression {r = new Neg{line = $m.Line, column = $m.CharPositionInLine, Operand = e};}
| b=binary_expression {r = b;}
| l=lvalue a=ASSIGN e=expression  {r = new Assign{line = $a.Line, column = $a.CharPositionInLine, Source = e, Target = l};}
| i=ID L_PARENT e=expression_list? R_PARENT {r = new Call{line = $i.Line, column = $i.CharPositionInLine, Arguments = e, FunctionName = $i.text};}
| L_PARENT e=expression_sequence? R_PARENT {r = e}
| i=ID L_KEY f=field_list? R_KEY { r = new RecordDeclaration{line = $i.Line, column = $i.CharPositionInLine, Members = f, Lex = $i.text, TypeName = $i.text, Dependencies = ??, Pure = ??};}
| i=ID L_BRACKETS e1=expression R_BRACKETS OF e2=expression {r = new ArrayDeclaration{line = $i.Line, column = $i.CharPositionInLine, ArrayOf = $e2.text, Lex = $i.text, TypeName = $i.text, Dependencies = ??, Pure = ??};}
| i=IF e1=expression THEN e2=expression {r = new IfThenElse{line = $i.Line, column = $i.CharPositionInLine, If = e1, Then = e2};}
| i=IF e1=expression THEN e2=expression ELSE e3=expression {r = new IfThenElse{line = $i.Line, column = $i.CharPositionInLine, If = e1, Then = e2, Else = e3};}
| w=WHILE e1=expression DO e2=expression {r = new While{line = $w.Line, column = $w.CharPositionInLine, Condition = e1, Body = e2};}
| f=FOR i=ID ASSIGN e1=expression TO e2=expression DO e3=expression {r = new BoundedFor{line = $f.Line, column = $f.CharPositionInLine, VarName = $i.text, From = e1, To = e2, Body = e3};}
| b=BREAK {r = new Break{line = $b.Line, column = $b.CharPositionInLine};}
| l=LET d=declaration_list IN e=expression_sequence END {r = new Let{line = $l.Line, column = $l.CharPositionInLine, Declarations = d, Body = e};};

binary_expression returns [IExpresion r]
:'?';

expression_list returns [List<IExpression> r]
@init
{
	r =  new List<IExpression>();
}
: e=expression {r.Add(e);} (COMMA e1= expression {r.Add(e1);})*; 



declaration_list_list returns [DeclarationListList<IDeclaration> r]
@init
{
	r = new DeclarationListList<IDeclaration>();
}
:(dl=declaration_list{r.Add(dl);})+;

declaration_list returns [DeclarationList<IDeclaration> r]
: type_declaration_list
| function_declaration_list
| var_declaration_list;
	 
type_declaration_list returns [TypeDeclarationList r]
@init
{
	r = new TypeDeclarationList();
}
:(t=type_declaration{r.Add(t);})+;

expression_sequence returns[IExpression r]
@init
{
	r = new ExpressionList<IExpression>()
}
: e1=expression{r.Add(e1);} ( SEMICOLON e2=expression{r.Add(e1);})*;

field_list returns[List<Tuple<string, string>> r]
@init
{
	r = newList<Tuple<string, string>>();
}
: i=ID ASSIGN e=expression{r.Add(new Tuple<string, string>(i.text, e.Lex) )} (COMMA i=ID ASSIGN e=expression)*

function_declaration_list returns [FunctionDeclarationList r]
@init
{
	r = new FunctionDeclarationList();
}
:(t=function_declaration{r.Add(t);})+;

var_declaration_list returns [DeclarationList<VarDeclaration> r]
@init
{
	r = new DeclarationList<VarDeclaration>();
}
:(t=var_declaration{r.Add(t);})+;

function_declaration returns [FunctionDeclaration r]
: //FUNCTION i=ID L_PARENT p=type_fields? R_PARENT ASSIGN e=expression{}
 FUNCTION i=ID L_PARENT p=type_fields? R_PARENT (COLON t=ID)? ASSIGN e=expression{ r = new FunctionDeclaration{line = $i.Line, column = $i.CharPositionInLine, Parameters = p, Return = t, FunctionName = i.text, Lex = i.text, Body = e};};
;

var_declaration returns [VarDeclaration r]
: v=VAR i=ID ASSIGN e=expression {r = new VarDeclaration {line = $v.Line, column = $v.CharPositionInLine, HolderName = $i.text, Init = e};}
| v=VAR i=ID COLON ti=ID ASSIGN e=expression {r = new VarDeclaration {line = $v.Line, column = $v.CharPositionInLine, HolderName = $i.text, HolderType = $ti.text, Init = e};}
;	
	
type_declaration returns [TypeDeclaration r]
: t=TYPE i=ID EQUAL t = type {t.TypeName = $i.text; t.line = $t.line; t.column = $t.CharPositionInLine; r = t;}
;

type returns [TypeDeclaration r]
: a=ARRAY OF i=ID {r = new ArrayDeclaration{ArrayOf = $i.text};}
| i=ID {r = new AliasDeclaration{AliasOf = $i.id};}	
;

type_fields returns[List<ParameterDeclaration> r]
@init
{
	r = new List<ParameterDeclaration>();
	int c = 0;
}
: (t=type_field{t.Position = c; r.Add(t}); c++})+;

type_field returns[ParameterDeclaration r]
: i=ID COLON t=ID {r = new ParameterDeclaration{line = $i.Line, column = $i.CharPositionInLine, HolderName = $i.text, HolderType = $t.text};};


lvalue returns[IExpression r]
: i1=ID{r = new Var{Name=$i1.text};} (DOT i2=ID{r = new MemberAccess{ MemberName = $i2.text, Record = r};} |  L_BRACKETS e=expression R_BRACKETS{r = new ArrayAccess{ Indexer = e, Array = r};})*;

