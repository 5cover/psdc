# Notable additions

In order to make Pseudocode usable, a few features that are not present in the taught, "official" flavors of Pseudocode have been added.

These features should probably be avoided in tests.

This document provides an exhaustive list of these "notable additions".

## Type conversions

Sometimes it is necessary to convert values from one type to another. This can be done with a C-like syntax for type casting, or through implicit conversions. [More details](Conversions.md)

## Directives

Directives (lines starting with `#`) are used for configuration, comptime introspection, and modularity.

## Primitive initializers

Initialiers for primitive types (any type except structure or arrays) are supported.

## NOP statements

Just a semi-colon `;`. Does nothing. Not officially allowed.
