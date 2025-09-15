parser grammar Pseudocode23Parser;

options {
    tokenVocab = Pseudocode23Lexer;
}

// Entry

algorithm: program? EOF;

program: Programme Ident Is declaration*;

block: statement*;

// Declarations

declaration:
    compiler_directive
    | constant_decl
    | function_decl
    | function_def
    | main_program
    | nop
    | procedure_decl
    | procedure_def
    | type_alias;

constant_decl: Constant type Ident ColonEqual expr Semi;

function_decl: function_signature Semi;

function_def: function_signature Is? Begin block End;

main_program: Begin block End;

procedure_decl: procedure_signature Semi;

procedure_def: procedure_signature Is? Begin block End;

type_alias: Type Ident Equals type Semi;

// Statements

statement:
    alternative
    | assignment
    | builtin_assigner
    | builtin_ecrire
    | builtin_ecrire_ecran
    | builtin_fermer
    | builtin_lire
    | builtin_lire_clavier
    | builtin_ouvrir_ajout
    | builtin_ouvrir_ecriture
    | builtin_ouvrir_lecture
    | compiler_directive
    | do_while_loop
    | for_loop
    | local_variable
    | nop
    | repeat_loop
    | return_stmt
    | switch_stmt
    | while_loop
    | expr Semi;

alternative: alternative_if alternative_elseif* alternative_else? EndIf;

alternative_if: If expr Then block;

alternative_elseif: ElseIf expr Then block;

alternative_else: Else block;

assignment: lvalue Equals expr Semi;

builtin_assigner: Assign LParen lvalue Comma expr RParen Semi;

builtin_ecrire: Write LParen expr Comma expr RParen Semi;

builtin_ecrire_ecran: WriteScreen LParen expr_list? RParen Semi;

builtin_fermer: Close LParen expr RParen Semi;

builtin_lire: Read LParen expr Comma lvalue RParen Semi;

builtin_lire_clavier: ReadKeyboard LParen lvalue RParen Semi;

builtin_ouvrir_ajout: OpenAppend LParen expr RParen Semi;

builtin_ouvrir_ecriture: OpenWrite LParen expr RParen Semi;

builtin_ouvrir_lecture: OpenRead LParen expr RParen Semi;

do_while_loop: Do block While expr;

for_loop: (For for_loop_content | For LParen for_loop_content RParen) Do block EndDo;
for_loop_content: Ident From expr To expr ( Step expr)?;

local_variable: variable_declaration ( ColonEqual initializer)? Semi;

repeat_loop: Repeat block Until expr;

return_stmt: Return expr? Semi;

switch_stmt: Switch expr Is switch_case+ EndSwitch;

switch_case: When expr Arrow statement* | When Other Arrow statement*;

while_loop: While expr Do block EndDo;

// Expressions (precedence climbing)

expr: expr_or;

expr_or: expr_and ( Or expr_and)*;

expr_and: expr_xor ( And expr_xor)*;

expr_xor: expr_equality ( Xor expr_equality)*;

expr_equality: expr_comparison ( ( NotEqual | Equal) expr_comparison)*;

expr_comparison:
    expr_addsub (( Less | LessEqual | Greater | GreaterEqual) expr_addsub)*;

expr_addsub: expr_muldimod ( ( Minus | Plus) expr_muldimod)*;

expr_muldimod: expr_unary ( ( Star | Slash | Percent) expr_unary)*;

expr_unary: (Minus | Plus | Not | LParen type RParen) expr_unary
    | expr_primary;

expr_primary: lvalue | terminal_rvalue;

// Lvalues and terminals

lvalue:
    terminal_rvalue indexer+         // ArraySubscript
    | terminal_rvalue member_access+ // ComponentAccess
    | terminal_lvalue;               // bare lvalue

terminal_lvalue:
    LParen lvalue RParen // BracketedLvalue
    | Ident;             // IdentReference

terminal_rvalue:
    LParen expr RParen                           // Bracketed
    | Feof LParen expr RParen                     // BuiltinFdf
    | Ident LParen parameter_actual_list? RParen // Call
    | LiteralCharacter
    | LiteralInteger
    | LiteralReal
    | LiteralString
    | True
    | False
    | terminal_lvalue;

// Types

type:
    Ident                             // alias reference
    | Array indexer_spec+ From type // array
    | Boolean
    | Char
    | File
    | Int
    | Real
    | String                               // plain string type
    | String LParen expr RParen            // string(length)
    | Struct Begin component_decl+ End; // structure début ... fin

component_decl: compiler_directive | variable_declaration Semi;

// Other constructs

compiler_directive:
    Hash Assert expr expr? // #assert <expr> [<expr>]
    | Hash Eval Expr expr  // #eval expr <expr>
    | Hash Eval Type type; // #eval type <type>

parameter_formal: ( EntF | SortF | EntFSortF) Ident Colon type;

parameter_actual: ( EntE | SortE | EntESortE) expr;

variable_declaration: ident_list Colon type;

function_signature:
    Fonction Ident LParen parameter_formal_list? RParen Delivers type;

procedure_signature: Procedure Ident LParen parameter_formal_list? RParen;

nop: Semi;

initializer: expr | braced_initializer;

braced_initializer: LBrace braced_item_list? RBrace;

braced_item_list: braced_item ( Comma braced_item)* Comma?;

braced_item: ( designator+ ColonEqual)? initializer | compiler_directive;

designator: member_access | indexer;

member_access: Dot Ident;

indexer: LBracket expr_list RBracket;

indexer_spec: LBracket expr RBracket;

// Helpers for comma-lists (allow trailing comma)

expr_list: expr ( Comma expr)* Comma?;

parameter_formal_list: parameter_formal ( Comma parameter_formal)* Comma?;

parameter_actual_list: parameter_actual ( Comma parameter_actual)* Comma?;

ident_list: Ident ( Comma Ident)* Comma?;