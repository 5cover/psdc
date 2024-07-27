// Generated automatically by nearley, version 2.20.1
// http://github.com/Hardmath123/nearley
(function () {
function id(x) { return x[0]; }

const moo = require("moo");

const lexer = moo.compile({
    mdMath: '$$',
    nl: /\\\\/,
    ws: {match: /\\ |\s/, lineBreaks: true},
    amp: '&',
    braceL: '{',
    braceR: '}',
    zeroOrOne: '?',
    zeroOrMore: '*',
    oneOrMore: '+',
    text: {match: /\\text\s*?{(?:.(?<![^\\]}))+}/, lineBreaks: true},
    csl: '\\#', // comma-separated list
    header: {match: /\\textbf\s*?{(?:.(?<![^\\]}))+}/, lineBreaks: true},
    to: '\\to',
    caret: '^',
    alignL: {match: /\\begin\s*?{align\*}/, lineBreaks: true},
    alignR: {match: /\\end\s*?{align\*}/, lineBreaks: true},
    casesL: {match: /\\begin\s*?{Bmatrix\*}\[l\]/, lineBreaks: true},
    casesR: {match: /\\end\s*?{Bmatrix\*}/, lineBreaks: true},
    groupL: {match: /\\begin\s*?{pmatrix}/, lineBreaks: true},
    groupR: {match: /\\end\s*?{pmatrix}/, lineBreaks: true},
    splitL: {match: /\\begin\s*?{split}/, lineBreaks: true},
    splitR: {match: /\\end\s*?{split}/, lineBreaks: true},
    ruleName: {match: /⟨[^⟩]+⟩/, lineBreaks: true},
    tokenName: /[a-zA-Z0-9_]/
})
var grammar = {
    Lexer: lexer,
    ParserRules: [
    {"name": "syntax$ebnf$1", "symbols": []},
    {"name": "syntax$ebnf$1$subexpression$1", "symbols": ["topLevelElement", "_nl"]},
    {"name": "syntax$ebnf$1", "symbols": ["syntax$ebnf$1", "syntax$ebnf$1$subexpression$1"], "postprocess": function arrpush(d) {return d[0].concat([d[1]]);}},
    {"name": "syntax", "symbols": ["_nl", (lexer.has("mdMath") ? {type: "mdMath"} : mdMath), "_nl", (lexer.has("alignL") ? {type: "alignL"} : alignL), "_nl", "syntax$ebnf$1", (lexer.has("alignR") ? {type: "alignR"} : alignR), "_nl", (lexer.has("mdMath") ? {type: "mdMath"} : mdMath), "_nl"]},
    {"name": "topLevelElement$subexpression$1", "symbols": [(lexer.has("header") ? {type: "header"} : header)]},
    {"name": "topLevelElement$subexpression$1", "symbols": ["rule"]},
    {"name": "topLevelElement", "symbols": [(lexer.has("amp") ? {type: "amp"} : amp), "_", "topLevelElement$subexpression$1"]},
    {"name": "rule$ebnf$1$subexpression$1", "symbols": ["_", "item"]},
    {"name": "rule$ebnf$1", "symbols": ["rule$ebnf$1$subexpression$1"]},
    {"name": "rule$ebnf$1$subexpression$2", "symbols": ["_", "item"]},
    {"name": "rule$ebnf$1", "symbols": ["rule$ebnf$1", "rule$ebnf$1$subexpression$2"], "postprocess": function arrpush(d) {return d[0].concat([d[1]]);}},
    {"name": "rule", "symbols": [(lexer.has("ruleName") ? {type: "ruleName"} : ruleName), "_", (lexer.has("to") ? {type: "to"} : to), "rule$ebnf$1"]},
    {"name": "item$subexpression$1", "symbols": ["cases"]},
    {"name": "item$subexpression$1", "symbols": ["group"]},
    {"name": "item$subexpression$1", "symbols": ["split"]},
    {"name": "item$subexpression$1", "symbols": [(lexer.has("ruleName") ? {type: "ruleName"} : ruleName)]},
    {"name": "item$subexpression$1", "symbols": ["terminal"]},
    {"name": "item$ebnf$1$subexpression$1", "symbols": ["_", "quantifier"]},
    {"name": "item$ebnf$1", "symbols": ["item$ebnf$1$subexpression$1"], "postprocess": id},
    {"name": "item$ebnf$1", "symbols": [], "postprocess": function(d) {return null;}},
    {"name": "item", "symbols": ["item$subexpression$1", "item$ebnf$1"]},
    {"name": "cases$ebnf$1$subexpression$1", "symbols": ["case", (lexer.has("nl") ? {type: "nl"} : nl), "_"]},
    {"name": "cases$ebnf$1", "symbols": ["cases$ebnf$1$subexpression$1"]},
    {"name": "cases$ebnf$1$subexpression$2", "symbols": ["case", (lexer.has("nl") ? {type: "nl"} : nl), "_"]},
    {"name": "cases$ebnf$1", "symbols": ["cases$ebnf$1", "cases$ebnf$1$subexpression$2"], "postprocess": function arrpush(d) {return d[0].concat([d[1]]);}},
    {"name": "cases", "symbols": [(lexer.has("casesL") ? {type: "casesL"} : casesL), "_", "cases$ebnf$1", (lexer.has("casesR") ? {type: "casesR"} : casesR)]},
    {"name": "case", "symbols": ["rule", "_"]},
    {"name": "case$ebnf$1$subexpression$1", "symbols": ["item", "_"]},
    {"name": "case$ebnf$1", "symbols": ["case$ebnf$1$subexpression$1"]},
    {"name": "case$ebnf$1$subexpression$2", "symbols": ["item", "_"]},
    {"name": "case$ebnf$1", "symbols": ["case$ebnf$1", "case$ebnf$1$subexpression$2"], "postprocess": function arrpush(d) {return d[0].concat([d[1]]);}},
    {"name": "case", "symbols": ["case$ebnf$1"]},
    {"name": "group$ebnf$1", "symbols": []},
    {"name": "group$ebnf$1$subexpression$1", "symbols": ["item", "_"]},
    {"name": "group$ebnf$1", "symbols": ["group$ebnf$1", "group$ebnf$1$subexpression$1"], "postprocess": function arrpush(d) {return d[0].concat([d[1]]);}},
    {"name": "group", "symbols": [(lexer.has("groupL") ? {type: "groupL"} : groupL), "_", "group$ebnf$1", (lexer.has("groupR") ? {type: "groupR"} : groupR)]},
    {"name": "split$ebnf$1$subexpression$1$ebnf$1$subexpression$1", "symbols": ["item", "_"]},
    {"name": "split$ebnf$1$subexpression$1$ebnf$1", "symbols": ["split$ebnf$1$subexpression$1$ebnf$1$subexpression$1"]},
    {"name": "split$ebnf$1$subexpression$1$ebnf$1$subexpression$2", "symbols": ["item", "_"]},
    {"name": "split$ebnf$1$subexpression$1$ebnf$1", "symbols": ["split$ebnf$1$subexpression$1$ebnf$1", "split$ebnf$1$subexpression$1$ebnf$1$subexpression$2"], "postprocess": function arrpush(d) {return d[0].concat([d[1]]);}},
    {"name": "split$ebnf$1$subexpression$1", "symbols": [(lexer.has("amp") ? {type: "amp"} : amp), "_", "split$ebnf$1$subexpression$1$ebnf$1", (lexer.has("nl") ? {type: "nl"} : nl), "_"]},
    {"name": "split$ebnf$1", "symbols": ["split$ebnf$1$subexpression$1"]},
    {"name": "split$ebnf$1$subexpression$2$ebnf$1$subexpression$1", "symbols": ["item", "_"]},
    {"name": "split$ebnf$1$subexpression$2$ebnf$1", "symbols": ["split$ebnf$1$subexpression$2$ebnf$1$subexpression$1"]},
    {"name": "split$ebnf$1$subexpression$2$ebnf$1$subexpression$2", "symbols": ["item", "_"]},
    {"name": "split$ebnf$1$subexpression$2$ebnf$1", "symbols": ["split$ebnf$1$subexpression$2$ebnf$1", "split$ebnf$1$subexpression$2$ebnf$1$subexpression$2"], "postprocess": function arrpush(d) {return d[0].concat([d[1]]);}},
    {"name": "split$ebnf$1$subexpression$2", "symbols": [(lexer.has("amp") ? {type: "amp"} : amp), "_", "split$ebnf$1$subexpression$2$ebnf$1", (lexer.has("nl") ? {type: "nl"} : nl), "_"]},
    {"name": "split$ebnf$1", "symbols": ["split$ebnf$1", "split$ebnf$1$subexpression$2"], "postprocess": function arrpush(d) {return d[0].concat([d[1]]);}},
    {"name": "split", "symbols": [(lexer.has("splitL") ? {type: "splitL"} : splitL), "_", "split$ebnf$1", (lexer.has("splitR") ? {type: "splitR"} : splitR)]},
    {"name": "terminal", "symbols": [(lexer.has("text") ? {type: "text"} : text)]},
    {"name": "terminal", "symbols": [(lexer.has("tokenName") ? {type: "tokenName"} : tokenName)]},
    {"name": "quantifier", "symbols": [(lexer.has("caret") ? {type: "caret"} : caret), "_", "quantifierSup"]},
    {"name": "quantifierSup", "symbols": [(lexer.has("zeroOrOne") ? {type: "zeroOrOne"} : zeroOrOne)]},
    {"name": "quantifierSup", "symbols": [(lexer.has("zeroOrMore") ? {type: "zeroOrMore"} : zeroOrMore)]},
    {"name": "quantifierSup", "symbols": [(lexer.has("oneOrMore") ? {type: "oneOrMore"} : oneOrMore)]},
    {"name": "quantifierSup", "symbols": [(lexer.has("braceL") ? {type: "braceL"} : braceL), "_", (lexer.has("zeroOrMore") ? {type: "zeroOrMore"} : zeroOrMore), "_", (lexer.has("csl") ? {type: "csl"} : csl), "_", (lexer.has("braceR") ? {type: "braceR"} : braceR)]},
    {"name": "quantifierSup", "symbols": [(lexer.has("braceL") ? {type: "braceL"} : braceL), "_", (lexer.has("oneOrMore") ? {type: "oneOrMore"} : oneOrMore), "_", (lexer.has("csl") ? {type: "csl"} : csl), "_", (lexer.has("braceR") ? {type: "braceR"} : braceR)]},
    {"name": "_$ebnf$1", "symbols": []},
    {"name": "_$ebnf$1", "symbols": ["_$ebnf$1", (lexer.has("ws") ? {type: "ws"} : ws)], "postprocess": function arrpush(d) {return d[0].concat([d[1]]);}},
    {"name": "_", "symbols": ["_$ebnf$1"], "postprocess": d=>null},
    {"name": "_nl$ebnf$1", "symbols": []},
    {"name": "_nl$ebnf$1$subexpression$1", "symbols": [(lexer.has("nl") ? {type: "nl"} : nl)]},
    {"name": "_nl$ebnf$1$subexpression$1", "symbols": [(lexer.has("ws") ? {type: "ws"} : ws)]},
    {"name": "_nl$ebnf$1", "symbols": ["_nl$ebnf$1", "_nl$ebnf$1$subexpression$1"], "postprocess": function arrpush(d) {return d[0].concat([d[1]]);}},
    {"name": "_nl", "symbols": ["_nl$ebnf$1"], "postprocess": d=>null}
]
  , ParserStart: "syntax"
}
if (typeof module !== 'undefined'&& typeof module.exports !== 'undefined') {
   module.exports = grammar;
} else {
   window.grammar = grammar;
}
})();
