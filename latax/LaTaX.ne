#!/bin/env -S /usr/local/bin/nearleyc -o LaTaX.js
@{%
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
%}

@lexer lexer

syntax ->
    _nl %mdMath _nl %alignL _nl (topLevelElement _nl):* %alignR _nl %mdMath _nl

topLevelElement ->
    %amp _ (%header | rule)

rule ->
    %ruleName _ %to (_ item):+

item ->
    ( cases
    | group
    | split
    | %ruleName
    | terminal) (_ quantifier):?

cases -> %casesL _ (case %nl _):+ %casesR

case -> rule _
      | (item _):+

group -> %groupL _ (item _):* %groupR

split -> %splitL _ (%amp _ (item _):+ %nl _):+ %splitR

terminal -> %text
          | %tokenName

quantifier ->
    %caret _ quantifierSup

quantifierSup -> %zeroOrOne
             | %zeroOrMore
             | %oneOrMore
             | %braceL _ %zeroOrMore _ %csl _ %braceR
             | %braceL _ %oneOrMore _ %csl _ %braceR

_ -> %ws:*
{%d=>null%}
_nl -> (%nl | %ws):*
{%d=>null%}