grammar LaTaX_parser;

options {
    tokenVocab = LaTaX_lexer;
}

syntax: ALIGN_L (HEADER | top_rule) (NL (HEADER | top_rule))* NL? ALIGN_R EOF;
top_rule: rule_name AMPERSAND RULE_DEF part+;
rule: rule_name RULE_DEF part+;
part: item modifier?
    | GROUP_L rule GROUP_R modifier
    | rule;
item: CASES_L IDENT (NL IDENT)* NL? CASES_R
    | rule
    | rule_name
    | GROUP_L item* GROUP_R
    | terminal;
terminal: TEXT
        | IDENT;
modifier: MODIFIER ( ZERO_OR_MORE LIST?
                   | ONE_OR_MORE LIST?
                   | ZERO_OR_ONE);
rule_name: RULE_L IDENT RULE_R;
