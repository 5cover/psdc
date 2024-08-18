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

Avoids memory allocations
