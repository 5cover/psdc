# PSDC (WIP)

IUT de Lannion Pseudocode compiler (transpiler).

## Tools

[VSCode Pseudocode extension](https://marketplace.visualstudio.com/items?itemName=NoanPerrot.pseudocode)

## Philosophy

Psdc is a transpiler, designed to automate the painstaking task of rewriting Pseudocode programs to other languages. The goal is, given any (valid or invalid) Pseudocode program to produce an equivalent program in the target language.

Equivalent here means:

- Equivalence in validity: a valid Pseudocode program must transpile to a valid program in the target language. An invalid program (with compiler errors) could not be valid in the target language coincidentally.
- Equivalence in behavior: a valid Pseudocode program must exert the expected behavior in the target language. Since Pseudocode programs can't be executed, the expected behavior of a program is determined by reading it.
- Equivalence in semantics: a Pseudocode program and its transpiled counterpart must be semantically equivalent. This means:
    - using the same identifier names (except where target languages keywords are used)
    - using the same order in declarations (as possible considering the rules of the target language)

The target language code must be ready for human reading and modification, and symmetrical with the input (i.e. it must be easy to see to which part of the input corresponds each part of the output, and vice versa).

## Roadmap

### Target languages

- [x] C
- [ ] LLVM
- [ ] C#
- [ ] CimPU
- [ ] Java
- [ ] JavaScript
- [ ] Pascal
- [ ] Perl
- [ ] PHP
- [ ] Python
- [ ] Shell
- [ ] SQL

### Language features

Notation: bold &rarr; underway

- [x] Alternatives
- [x] Loops
    - [x] For
    - [x] While
    - [x] Do..While
    - [x] Repeat..Until
- [x] Procedures
- [x] Functions
- [x] Structures
- [x] `selon`
- [x] Fix syntax error handling
- [x] Lvalues
- [x] Constant folding for type checking and division by zero
- [x] Optional brackets in control structures
- [x] Benchmarks
- [ ] **Formal grammar**
- [ ] **Brace initialization (see TD14 ex 1)**
- [ ] More static analysis
- [ ] `finPour` keyword (equivalent to `fin`)
- [ ] Escape sequences in string and character literals
- [ ] Lowercase boolean operators
- [ ] Modules
- [ ] Numeroted control stuctures (`si1`, `si2`, `si3`)
- [ ] File handling (low priority)
- [ ] Preprocessor (limitations of the language)
    - [ ] `ecrireEcran` newlines
- [ ] [GNU](https://www.gnu.org/prep/standards/standards.html#Errors)-compliant message formatting
- [ ] CLI (use nuget package)
    - [ ] custom header
    - [ ] -d, --documentation: none, all, file?
    - [ ] Global preprocessor directives
    - [ ] Formatting customization
    - [ ] documentation date: now, file?
- [ ] Tests
    - [ ] Errors
    - [ ] Valid code
- [ ] Documentation
    - [ ] CLI
    - [ ] Language standard
- [ ] Initial release
- [ ] Self-hosting (rewrite in Pseudocode)
- [ ] VSCode tooling
    - [ ] Debugger
    - [ ] Language server

### C output configuration

- [ ] Non-null-terminated-string-proof format strings: width specifier for lengthed strings (usually useless since null-terminated, but could be useful if non null-terminated strings are used)
- [ ] Type mappings
    - [ ] `réel` &rarr; `float`, `double`, `long double`?
    - [ ] `entier` &rarr; `short`, `int`, `long`?
    - [ ] `caractère` &rarr; `char`, `tchar_t`, `wchar_t`?
- [ ] Parameter names in prototypes?
- [ ] Doxygen documentation skeleton?
- [ ] `i++` or `++i`
