# Implémentation

Maintain a collection of tokens.

Each should try to parse itself.

If it succeeds, the input is consumed, and it returns a parameterized clone of itself. The caller adds it to the list.
If it fails, the caller moves on to the next token.

## Tokens

**Token** : atomic sytnax element.

A character part of a string literal cannot be a token as it loses meaning without the quote delimiters.

A token isn't valid code on its own.

A token starts with the first character of the input or with the first valid character.

A token ends with the last character of the input or with the last valid character.

Token types:

### Keyword

Reserved words that cannot be used as identifiers.

Diacritics are optional. (délivre == delivre)

#### General

- c'est
- début
- fin
- programme
- type
- structure

### Data

- faux
- vrai
- constante
- tableau
- de

#### Subroutines

- délivre
- fonction
- procédure
- retourne
- écrireÉcran | écrire
- lireClavier | lire

#### Control structures

- alors
- faire
- finfaire
- finsi
- pour
- quand
- selon
- finselon
- si
- sinon
- tant que
- à
- pas

#### Parameters

- entE
- entE/sortE
- entF
- entF/sortF
- sortE
- sortF

### Identifier

Only contains alphanumerical characters or an underscore ('_').

May not start with a digit.

### Binary operator

#### Arithmetic

value|description
-|-
`a+b`|plus
`a-b`|minus
`a/b`|division
`a*b`|multiplication
`a%b`|modulus

#### Logical (case insensitive)

value|description
-|-
`a ET b`|and
`a OU b`|or
`NON a`|not

#### Comparison

value|description
-|-
`a<b`|less than
`a>b`|greater than
`a<=b`|less than or equal
`a>=b`|greater than or equal
`a==b`|equals
`a!=b`|not equal

#### Assignment

value|description
-|-
`a:=b`|assignment

#### Member access

value|description
-|-
`a[b]`|array subscript
`a.b`|member access

### Integer literal

Only contain digits.

### Real literal

Two integer literals separated by a dot.

### String literal

Any characters enclosed in double quotes.

### Character literal

Any character enclosed in simple quotes.

### Single-line comment

`// ... <newline>`

### Multi-line comment

`/* ... */`

### Punctuator

value|description
-|-
`=>`|case statement
`;`|declaration/statement terminator
`,`|parameter separator

### Brace

Either opening ('{') or closing ('}')

### Bracket

Either opening ('(') or closing (')')

### Square bracket

Either opening ('[') or closing (']')

## How to tokenize?

Regex :

token|pattern|flags
-|-|-
Singeline comment |`//(.*)$` | Multiline
Multiline comment |`/\*(.*?)\*/` | Singleline
Identifier        |`([a-zA-Z_]\w*)`|
Integer literal   |`(\d+)`|
Real literal      |`(\d*\.\d+)`|
String literal    |`"(.*?)"`|
Character literal |`'(.)'`|

Instead of defining a million TokenTypes, what we could to is use regex to identify code structures :

Like for an array declaration

`tableau(?:\s*\[\s*(\w+)\s*\])+\s*de\s+([a-zA-Z_]\w*)`

We could make such regexes for :

- function signature `fonction\s+\((entF|sortF|entF/sortF
)*\)`
    - name
    - parameters
        - mode
        - name
        - type
    - return type
- procedure signature
    - name
    - parameters
        - mode
        - name
        - type
- variable declaration
    - name
    - type
- constant declaration
    - type
    - name
    - value
- Program declaration
    - name

Other composed like si, pour, function call, not really as they take expressions

Maybe this is a bad idea because they don't offer the possibility of meanigful syntactic and semantic errors.
