# Message ideas

## Syntax errors

### Parameter modes

- Actual parameter mode where formal is expected
- Formal parameter mode where actual is expected
- Missing parameter mode
    - In call
    - In procedure/function declaration/definition
- Actual parameter mode mismatch

## Semantic errors

- Redefinition of symbol
    - Function
    - Procedure
    - Variable
    - Type alias
- Two MainPrograms
- Function/Procdeure Signature mismatch (between prototypes or between definition/prototype)
- Reassignment of constant
- Output parameter never assigned
- Value of output parameter is undefined here
- ~~`lire[Clavier]`, `ecrire[Ecran]` to/of an expression of a type for which it doesn't make sense to do so (`nomFichierLog`). We won't define custom IO logic that C doesn't natively support, it would be up to the user to do so.~~
- ~~Assignment types incompatible~~
- ~~Unary expression unsupported operation for type A~~
- ~~Binary expression unsupported operation for types A, B~~
- ~~Array index out of range~~
- Reading unassigned variable
- for loop variant type doesn't support addition
- ~~Expected a value for return in function~~
- ~~No value was expected for return in procedure/mainprogram~~
- ~~Wrong type for return value in function~~

## Warnings

Warning : when the code may not work because you're doing something valid but highly unusual/unsupported that is undoubtedly wrong.

- Using file after close
- Reading to write-only file
- Writing to read-only file
- Using file before open
- File never closed
- File closed twice
- Unopened file closed

- Unreachable code (statements after return)
- Missing return from function

## Suggestions

Suggestion : style and formatting guidelines, code quality fixes, harmless mistakes

- Identifier names:
    - constants UPPER_CASE
    - types *t*PascalCase
    - structre components *c_*camelCase
    - others camelCase

- useless assignment (`1 := 1`)

## Specific to the target language

### C

- (ident type) `char` has been renamed to `char1` as `char` is a reserved C keyword.

### Other

- (ident type) `vaR` has been renamed to `var1` as the name `vaR` differs only in case from (ident type) `var`. This is not supported in language X.
- (ident type) `vaR` has been renamed to `var1` as the `vaR` differs only in case from X keyword `var`. This is not supported in language X.
