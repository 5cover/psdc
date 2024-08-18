# Possible performance optimizations

## Use RegexGenerator

Added in .NET 7, basically RegexOptions.Compiled but even faster.

**For**: RegexTokenRule

**Implementation**: Accept a Regex instance in RegexTokenRule instead of the pattern, use the RegexGenerator source generator.
