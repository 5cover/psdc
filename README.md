# PSDC (WIP)

IUT de Lannion Pseudocode compiler (transpiler).

## Tools

[VSCode Pseudocode extension](https://marketplace.visualstudio.com/items?itemName=NoanPerrot.pseudocode)

## Roadmap

### Target languages

- [x] C
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
- [ ] LLVM

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
- [ ] String literal escape sequences
- [ ] Lowercase boolean operators
- [ ] Modules
- [ ] Numeroted control stuctures (`si1`, `si2`, `si3`)
- [ ] File handling (low priority)
- [ ] Preprocessor (limitations of the language)
    - [ ] `ecrireEcran` newlines
- [ ] [GNU](https://www.gnu.org/prep/standards/standards.html#Errors)-compliant message formatting
- [ ] CLI (use nuget package)
    - [ ] custom header
    - [ ] -d, --documentation : none, all, file
    - [ ] Global preprocessor directives
    - [ ] Formatting customization
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
