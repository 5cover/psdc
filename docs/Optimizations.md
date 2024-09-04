# Possible performance optimizations

## Use RegexGenerator

Added in .NET 7, basically RegexOptions.Compiled but even faster.

**For**: RegexTokenRule

**Implementation**: Accept a Regex instance in RegexTokenRule instead of the pattern, use the RegexGenerator source generator.

## CA1859 refactor

Self-explanatory

## Multiparsing separator array

Instead of creating a list to hold the parsed items, count number of separators and create an array

## Use structs where appropriate

Avoids memory allocations.

## ParseOperation: don't reparse item after selecting in dictionary

For parsing of TokenTypes and Identifiers values

We do that to make parsers more reusable.

## Covariant option monad: goodbye

It's preventing it from being a value type. It forces memory allocation.

I wonder if we'd get better performance by converting the payload manually?

## Pass state to lambda consumers to avoid closures

This avoids the declaration and instanciation of a class.
