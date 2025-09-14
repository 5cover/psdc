parser grammar Pseudocode24Parser;

options {
    tokenVocab = Pseudocode24Lexer;
}

algorithm: decl* EOF;

// Declarations

decl
    : compiler_directive
    | constant_def
    | type_def
    | main_program
    | func_decl
    | func_def
    | nop
;

compiler_directive
    : Hash HashAssert expr expr?
    | Hash HashEval EvalExpr expr
    | Hash HashEval Type type
;

constant_def: Constant type Colon init_declarators Semi;

type_def: Type type Colon idents Semi;

main_program: Program Ident block;

func_decl: (func_sig | proc_sig) Semi;
func_def: (func_sig | proc_sig) block;

func_sig: Function Ident LParen formal_params RParen Returns type;
proc_sig: Procedure Ident LParen formal_params RParen;

formal_params: (formal_param Comma)* formal_param?;
formal_param: Out? type Colon Ident;

// Statements

stmt
    : compiler_directive
    | block
    | expr_stmt
    | assignment
    | while_loop
    | do_while_loop
    | for_loop
    | return
    | write
    | read
    | trunc
    | local_var_decl
    | alternative
    | switch
    | nop
;

block: Begin stmt* End;
expr_stmt: expr Semi;
assignment: lvalue ColonEqual expr Semi;
while_loop: While paren_expr Do stmt* EndWhile;
do_while_loop: Do stmt* While paren_expr Semi;
for_loop
    : For LParen assignment? Semi expr? Semi assignment? RParen Do stmt* EndFor
;
return: Return expr Semi;
write: Write LParen (expr Comma)* expr? RParen Semi;
read: lvalue (Equal | ColonEqual) Read LParen RParen Semi;
trunc: Trunc paren_expr Semi;
local_var_decl: type Colon declarators Semi;

alternative
    : If alternative_clause (
        ElseIf alternative_clause
    )* (
        Else stmt*
    )? EndIf
;
alternative_clause: paren_expr Then stmt*;

switch: Switch paren_expr Do switch_case* switch_default? EndSwitch;
switch_case: When expr (Or expr)* Arrow stmt*;
switch_default: WhenOther Arrow stmt*;

// Identifiers and declarators

idents: Ident (Comma Ident)* Comma?;

declarators: (declarator Comma)* declarator Comma?;
declarator: Ident (ColonEqual init)?;

init_declarators: (init_declarator Comma)* init_declarator Comma?;
init_declarator: Ident ColonEqual init;

init: expr | braced_init;
braced_init: LBrace ( braced_init_item Comma)* braced_init_item? RBrace;
braced_init_item: compiler_directive | (designator+ ColonEqual)? init;
designator: component | indice;

// Expressions

expr: expr_and (Or expr_and)*;
expr_and: expr_xor (And expr_xor)*;
expr_xor: expr_equality (Xor expr_equality)*;
expr_equality: expr_relational ((Eq | Neq) expr_relational)*;
expr_relational: expr_add ((Gt | Ge | Lt | Le) expr_add)*;
expr_add: expr_mult ((Minus | Plus) expr_mult)*;
expr_mult: expr_unary ((Mul | Div | Mod) expr_unary)*;
expr_unary: op_unary* expr_primary;
expr_primary: lvalue | terminal_rvalue;

op_unary: Plus | Minus | Not | LParen type RParen;

lvalue: array_sub | component_access | terminal_lvalue;
array_sub: terminal_rvalue indice+;
component_access: terminal_rvalue component+;

terminal_lvalue
    : LParen lvalue RParen # paren_lvalue
    | Out? Ident           # var_ref
;

terminal_rvalue: paren_expr | terminal_lvalue | call | literal;
call: Ident LParen (effective_param Comma)* effective_param? RParen;
effective_param: Out? expr;

literal
    : True
    | False
    | LiteralString
    | LiteralReal
    | LiteralCharacter
    | LiteralInteger
;

// Types

type
    : Ident                                 # aliased
    | Boolean                               # boolean
    | Character                             # character
    | Integer                               # integer
    | Real                                  # real
    | String paren_expr?                     # string
    | Array LParen type RParen indice+      # array
    | Structure Begin structure_member* End # structure
;
structure_member: compiler_directive | structure_component;
structure_component: type Colon idents Semi;

// Utils

indice: LBracket expr RBracket;
component: Dot Ident;
paren_expr: LParen expr RParen;
nop: Semi;