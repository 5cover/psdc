lexer grammar Pseudocode24Lexer;

Ws: [\p{White_Space}]+ -> skip;
Comment: ('/*' .*? '*/' | '//' ~[\n] '\n') -> skip;

Ident: [\p{L}_][\p{L}_0-9]*;

HashAssert: '#assert';
HashEvalExpr: '#eval_expr';
HashEvalType: '#eval_type';

Array: 'tableau';
Begin: 'début' | 'debut';
Boolean: 'booléen' | 'booleen';
Character: 'caractère' | 'caractere';
Constant: 'constante';
Do: 'faire';
Else: 'sinon';
ElseIf: 'sinon_si';
End: 'fin';
EndFor: 'fin_pour';
EndIf: 'fin_si';
EndSwitch: 'fin_selon';
EndWhile: 'fin_tant_que';
False: 'faux';
For: 'pour';
Function: 'fonction';
If: 'si';
Integer: 'integer';
Out: 'sortie';
Procedure: 'procédure' | 'procedure';
Program: 'programme';
Read: 'lire';
Real: 'réel';
Return: 'retourne';
Returns: 'délivre' | 'delivre';
String: 'chaîne' | 'chaine';
Structure: 'structure';
Switch: 'selon';
Then: 'alors';
True: 'vrai';
Trunc: 'ent';
Type: 'type';
When: 'quand';
WhenOther: 'quand_autre';
While: 'tant_que';
Write: 'écrire' | 'ecrire';

And options { caseInsensitive = true; }: 'et';
Not options { caseInsensitive = true; }: 'non';
Or options { caseInsensitive = true; }: 'ou';
Xor options { caseInsensitive = true; }: 'xor';

LBrace: '{';
LBracket: '[';
LParen: '(';
RBrace: '}';
RBracket: ']';
RParen: ')';

Arrow: '=>';
Colon: ':';
ColonEqual: ':=';
Comma: ',';
Div: '/';
Dot: '.';
Semi: ';';
Eq: '==';
Equal: '=';
Ge: '>=';
Gt: '>';
Le: '<=';
Lt: '<';
Minus: '-';
Mod: '%';
Mul: '*';
Neq: '!=';
Plus: '+';

LiteralCharacter: '\'' CChar+ '\'';
LiteralInteger: NonZeroDeigit Digit*;
LiteralReal: Digit* Dot Digit+;
LiteralString: '"' SChar+ '"';

fragment CChar: ~['\\\r\n] | EscapeSequence;
fragment SChar
    : ~["\\\r\n]
    | EscapeSequence
    | '\\\n'   // Added line
    | '\\\r\n' // Added line
;
fragment EscapeSequence: SimpleEscape | OctEscape | HexEscape | UnicodeEscape;
fragment SimpleEscape: '\\' ['"abfnrtv\\];
fragment OctEscape: '\\' OctDigit OctDigit? OctDigit?;
fragment HexEscape: '\\x' HexDigit+;
fragment OctDigit: [0-7];
fragment Digit: [0-9];
fragment NonZeroDeigit: [1-9];
fragment HexDigit: [0-9a-fA-F];
fragment UnicodeEscape: '\\u' HexQuad | '\\U' HexQuad HexQuad;
fragment HexQuad: HexDigit HexDigit HexDigit HexDigit;