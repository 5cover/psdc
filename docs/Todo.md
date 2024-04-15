# Todo list

Project roadmap:

- Brace initialization (voir TD4 ex 1)
- Alternative keywords
    - finPour in place of fin
- Numeroted control stuctures? si1, si2, si3

- ~~Alternatives~~
- ~~Loops~~
    - ~~For~~
    - ~~While~~
    - ~~Do..While~~
    - ~~Repeat..Until~~
- Procedures
- Functions
- Structures
- Files
- CLI
- Tests?
- Command arguments (use nuget package)
- Release
- Documentation

## Cleanup

- Refactor TypeInfo and others to isolate C-specific code
- Try to eliminate duplication (CodeGeneratorC.cs is a mess)
- semantic analysis pass that creates symbols and typeinfos before the code generation (so code generation needs no matchsome)

## Sample programs

- Prime sieve (find nth prime)
