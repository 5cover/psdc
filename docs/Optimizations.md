# Possible performance optimizations

## RegexTokenRule: Use RegexGenerator

Added in .NET 7, basically RegexOptions.Compiled but even faster.

**Affects**: Tokenization

**Implementation**: Accept a Regex instance in RegexTokenRule instead of the pattern, use the RegexGenerator source generator.

## CA1859 refactor

## Multiparsing separator array

Instead of creating a list to hold the parsed items, count number of separators and create an array

**Affects**: Parsing

## Use structs where appropriate

Avoids memory allocations.

## ParseOperation: don't reparse item after selecting in dictionary

For parsing of TokenTypes and Identifiers values

We do that to make parsers more reusable.

**Affects**: Parsing

## Covariant option monad: goodbye

It's preventing it from being a value type. It forces memory allocation.

I wonder if we'd get better performance by converting the payload manually?

## Pass state to lambda consumers to avoid closures

This avoids the declaration and instanciation of a class.

## Tokenization: use `ReadonlySpan<char>`

**Affects**: Tokenization

### Token rules: parse `ReadonlySpan<char>`

Requires a refactor of RegexTokenRule since EnumerateMatches can't handle groups ; pass in the inner regex, and the begin and end strings.

### Token: make *Value* a range in the input code

Avoids creating a string when it already exists inside the input code. Might require global access the the input. I don't see why that would be a problem. Not sure i'll need it though.

### Tokenizer: use the char array from `PreprocessLineContinuations` instead of creating a string from it

Means everything else has to be `ReadOnlySpan<char>`-ready. (see above).

## Dumb Ruled array

Instead of being "smart", *i.e.* separating token types into spearated classes and constructing the tokenrule array at runtime, make it a compite-time operation. Maybe using another program run during compilation? Or a source generator?

Because this operation could be determined at compile-time, it's just that C# doesn't have syntax for it.

But at the same time i want to keep the classes, it makes things clearer. I don't to improve performance at the cost of making the code obscure.
