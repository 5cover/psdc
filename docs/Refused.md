# Refused

Ideas that aren't planned be added or have been changed. Still keep them around as a reminder of why we did not add that.

## Semantic preprocessor directives

**Reason**: it is better to have a preprocessor syntax `#` that is interpreted before parsing. Compiler directives require static analysis and are a separate concept. Trying to use the same syntax for both would have been confusing. It's fine if they are not allowed everywhere, too. Top-level, statement and struct declarations is all we really need. They should feel like an instruction, like `static_assert` in C.

Instead of using compiler directives, keep the `#` syntax.
