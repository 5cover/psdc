# Done

## Building a failure-resilient ParseOperation

There are 3 possible outcomes of a parsing method:

- Success : keep going
- Light failure : return the failed result, keep going
- Critical failure : return the failed result, switch to the failure implementation

The failure either has to go to a place and only one place, either in the out ParseResult parameter, or in the return value.
We should make failures "light" as much as possible.

So for `ParseToken(TokenType type, out ParseResult<string> value)`, we will never return an ErroneousOperation and failure will be indicated by assigning value to a failed ParseResult.

But for `ParseToken(TokenType type)`, we don't have anywhere else for the value to go so we return an ErroneousOperation as we currenlty do.

## Multiple possibilities in parsing

Ok, so whenever there are multiple possibilities for a production rules, what we currently do is that
we peek the first token, and we call a specific parsing method depending on its type.

Currently, we do this for

- Statements
- Literals

There is also an alternative for parsing expressions, but we're using a different method:

We're calling the ParseResult.Else() extension method to try each alternative until one succeeds

This method combines the errors that occur.

## Complete types

Do we need to differentiate between complete types and incomplete types?

An incomplete type is basically an unlengthed string, a complete type is any other type.

For a type to be completes means that it specifies all required information to store an instance of it.

So an unlengthed string is not a compelete type because you don't know the length of the string, i.e. the number of bytes to allocate.

Sames goes for an array of unlenghted strings

In variable declarations we allow only complete types because that's when we allocate memory.

This distinction also exists in C:

```c
char str[10]; // complete
char *str; // incomplete

void func(
    char str[10],
    char *str // incomplete
)
{
    // ...
}
```

In Pseudocode:

```psc
str : chaîne(10); // ok
str : chaîne;     // interdit

procédure func(
    entF str : chaîne(10) // ok
    entF str : chaîne     // ok
)
```

The difference between a variable and a parameter declaration is that, in a variable declaration, we actually allocate the underlying data, whereas in a parameter declaration we only pass it, we don't instanciate anything. All we do is copy a pointer.

In a way parameters are consumers and variables are providers.

We do differ between complete and incomplete types, not in the types of nodes, but in the parsing.

The set of complete types is a subset of all types.

## For loops

Note : we assume that start <= end : `for (i = start; i <= end; i += step)`

if start > end, then step must be < 0 : `for (i = start; i >= end; i += step)`

Solution 1 : use safe version : `for (i = start; step < 0 ? i >= end : i <= end; i += step)`

**Solution 2** : disallow decreasing variant (but we won't be able to diagnose the issue, simply ignore the problem)

Solution 3 : require *step* to be a compile-time constant so we can choose to either `<=` or `>=` at compile-time instead of at run-time like Solution 1. This would require

- expression evaluation
- constant folding

## What constitutes to be a ParseResult

Rule for what belongs in a parse result:

Don't ParseResult collections, parse result the element type. Having collection properties basically means that we have a variadic node.

No parse result if the node can't exist without it such as when

- the only token parsed is a property (such as Literals)
- allowing a hole will result in too permissive parsing

Two kinds of properties

- Identity properties : the node is the property -> no ParseResult
- Composition properties : the node contains the property -> yes ParseResult

Examples

- Node.Expression.Literal.Integer(string Value) -> no ParseResult
- Node.Type.LengthedString(`ParseResult<Expression> Length`) -> yes ParseResult

Having a component encapsulated in a `ParseResult` in a node type means that we allow the outer node to exist even if this component has failed parsing. This prevents the parsing to stop for the node.

So this is mostly useful for "big" nodes like subroutines.

## "Helper" rules in formal grammar

Where to put "helper" rules in the formal grammar?

"Helper" rules are rules that carry no meaning on their own and are only used in the construction of more complex rules. Example: formal parameters.

They belong to the category of all the rules they are used to construct.
