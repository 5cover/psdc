# Tests

## Syntax errors

### Parameter modes

- Effective parameter mode where formal is expected
- Formal parameter mode where effective is expected
- Missing parameter mode
    - In call
    - In procedure/function declaration/definition
- Effective parameter mode mismatch

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
- Expected a value for return in function
- No value was expected for return in procedure/mainprogram
- Wrong type for return value in function

## Suggestions

Suggestion : style and formatting guidelines

- Identifier names:
    - constants UPPER_CASE
    - types *t*PascalCase
    - structre components *c_*camelCase
    - others camelCase
