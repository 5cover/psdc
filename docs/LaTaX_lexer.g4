lexer grammar LaTaX_lexer;

WS: [\p{White_Space}] -> skip;

AMPERSAND: '&';
ALIGN_L: '\\begin{align*}';
ALIGN_R: '\\end{align*}';
CASES_L: '\\begin{Bmatrix*}[l]';
CASES_R: '\\end{Bmatrix*}';
GROUP_L: '\\begin{pmatrix}';
GROUP_R: '\\end{pmatrix}';
BRACE_L: '{';
BRACE_R: '}';
IDENT: [a-zA-Z0-9]+;
MODIFIER: '^';
NL: '\\\\';
ONE_OR_MORE: '+';
RULE_DEF: '\\to ';
RULE_L: '⟨';
RULE_R: '⟩';
HEADER: '\\textbf' BRACE_L ~[}]+ BRACE_R;
LIST: '#';
TEXT: '\\text' BRACE_L ~[}]+ BRACE_R;
ZERO_OR_MORE: '*';
ZERO_OR_ONE: '?';