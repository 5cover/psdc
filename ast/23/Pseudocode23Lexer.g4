lexer grammar Pseudocode23Lexer;

options {
    caseInsensitive = true;
}

Ws: [\p{White_Space}]+                      -> skip;
Comment: ( '/*' .*? '*/' | '//' ~[\n] '\n') -> skip;

// Punctuation and operators

ColonEqual: ':=';
Arrow: '=>';
Equal: '==';
Equals: '=';
NotEqual: '!=';
LessEqual: '<=';
GreaterEqual: '>=';
Less: '<';
Greater: '>';
Plus: '+';
Minus: '-';
Star: '*';
Slash: '/';
Percent: '%';
Dot: '.';
Colon: ':';
Semi: ';';
Comma: ',';
LParen: '(';
RParen: ')';
LBracket: '[';
RBracket: ']';
LBrace: '{';
RBrace: '}';

// Keywords (put before Identifier)

Programme: 'programme';
Is: 'c\'est';
Begin: 'début' | 'debut';
End: 'fin';

Fonction: 'fonction';
Procedure: 'procédure' | 'procedure';
Delivers: 'délivre' | 'delivre';
Type: 'type';

Constant: 'constante';

If: 'si';
Then: 'alors';
ElseIf: 'sinonsi';
Else: 'sinon';
EndIf: 'finsi';

For: 'pour';
Do: 'faire';
EndDo: 'finfaire';

While: 'tant';
Switch: 'selon';
EndSwitch: 'finselon';
When: 'quand';
Repeat: 'répéter' | 'répeter' | 'repéter' | 'repeter';
Until: 'jusqu\'à' | 'jusqu\'a';

Return: 'retourne';

Or: 'OU';
And: 'ET';
Xor: 'XOR';
Not: 'NON';

True: 'vrai';
False: 'faux';

Array: 'tableau';
Boolean: 'booléen' | 'booleen';
Char: 'caractère' | 'caractere';
File: 'nomFichierLog';
Int: 'entier';
Real: 'réel' | 'reel';
String: 'chaîne' | 'chaine';
Struct: 'structure';

Assign: 'assigner';
Write: 'écrire' | 'ecrire';
WriteScreen: 'écrireÉcran' | 'ecrireEcran';
Close: 'fermer';
Read: 'lire';
ReadKeyboard: 'lireClavier';
OpenAppend: 'ouvrirAjout';
OpenWrite: 'ouvrirÉcriture' | 'ouvrirEcriture';
OpenRead: 'ouvrirLecture';
Feof: 'Feof';

Hash: '#';

// Parameter modes

EntF: 'entF';
SortF: 'sortF';
EntFSortF: 'entF/sortF';

EntE: 'entE';
SortE: 'sortE';
EntESortE: 'entE/sortE';

// Contextual keywords

Other: 'autre';
That: 'que';
Assert: 'assert';
Eval: 'eval';
Expr: 'expr';
From: 'de';
To: 'à' | 'a';
Step: 'pas';

// Identifier (Unicode letters and digits). After keywords.

Ident:
    Other
    | That
    | Assert
    | Eval
    | Expr
    | From
    | To
    | Step
    | [\p{L}_][\p{L}_0-9]*;
LiteralCharacter: '\'' CChar+ '\'';
LiteralInteger: NonZeroDigit Digit*;
LiteralReal: Digit* Dot Digit+;
LiteralString: '"' SChar+ '"';

fragment CChar: ~['\\\r\n] | EscapeSequence;
fragment SChar:
    ~["\\\r\n]
    | EscapeSequence
    | '\\\n'    // Added line
    | '\\\r\n'; // Added line
fragment EscapeSequence: SimpleEscape | OctEscape | HexEscape | UnicodeEscape;
fragment SimpleEscape: '\\' ['"abfnrtv\\];
fragment OctEscape: '\\' OctDigit OctDigit? OctDigit?;
fragment HexEscape: '\\x' HexDigit+;
fragment OctDigit: [0-7];
fragment Digit: [0-9];
fragment NonZeroDigit: [1-9];
fragment HexDigit: [0-9A-F];
fragment UnicodeEscape: '\\u' HexQuad | '\\U' HexQuad HexQuad;
fragment HexQuad: HexDigit HexDigit HexDigit HexDigit;