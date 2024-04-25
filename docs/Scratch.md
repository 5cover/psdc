# Scratch area

## Errors that go token after token

fix errors going one from one token:

so basically when a parsing error occurs in a parsing loop, what we currently do is move to the next token and resume parsing from there.

So whenever an error occurs, get a bunch of other errors for the following tokens, even though they were valid.
If ecrireEcran is mispelled, we get "expected ecrireEcran" for all tokens up to the final semi.

### Solution 1 : ParseError should have a property for the minimal amount of expected tokens

Then we can just skip that amount of tokens next time we parse.

tedious and prolly unreliable

### Solution 2 : who cares, it's not that big of a deal

it clutters the message list

### Solution 3 : skim until first token of the target node, then resume parsing

this could actually work. We will have to know that first expected token type for every one tho. Or we could pass it to the Parse(Zero|One)OrMore* method.

We need to give the multi-parsing method an answer to the question "what is the type of the first token of the thing that i want you to parse".

It will need to be a params to account for cases where there may be multiple first tokens. And we will need to add a new argument to multi-parsing method. This will clutter the code and increase duplication.

### Solution 4 : skim until valid or failure with different error expected tokens

`ParseError.IsEquivalent` because equality would incur comparison of ErroneousToken.

Requires comparison of ParseError, could work as well. Could result in missing errors if the errors have the same ExpectedTokens, they will get collapsed into one.

```text
programme HelloWorld c'est
début
    écrireEcran("")
    écrireEcran("")
    écrireEcran("")
    écrireEcran("")
fin
```

```text
[P0002] L 4, col 16: error: syntax error on Statement: expected PunctuationSemicolon; got 'écrireEcran'
        ---> écrireEcran("")
```

We get only one error for a missing semicolon with this algorithm (for `ParseZeroOrMoreUntilToken`)

```cs
while (!NextParsingTokenIsAny(endTokens).ValueOr(true)) {
    var item = parse(ParsingTokens);
    _readCount += Math.Max(1, item.SourceTokens.Count);

    if (item.HasValue || lastError is null || !item.Error.ExpectedTokens.SetEquals(lastError.ExpectedTokens)) {
        AddOrSyntaxError(items, item);
    }

    if (!item.HasValue) {
        lastError = item.Error;
    }
}
```

This seems to be the way GCC works.

```c
#include <stdio.h>

int main() {
    puts("")
    puts("")
    puts("")
    puts("")
}
```

```text
test.c: In function ‘main’:
test.c:4:13: error: expected ‘;’ before ‘puts’
    4 |     puts("")
      |             ^
      |             ;
    5 |     puts("")
      |     ~~~~ 
```

### Solution 5 : keep parsing after error in `ParseOperation` (implemented)

Right now when a `ParseOperation` switches to the failure implementation it stops reading tokens altogether.

This means the tokens that are part of the thing we were trying to parse but are after the error will not be read, and will cause another error when we parse something else. Example

```text
programme HelloWorld c'est
début
    si 1 = 0 alors

    finsi
fin
```

We attempted to parse an `Alternative` but encoundered an integer literal `1` instead of an open bracket `(`.

So we stop parsing the `Alternative` and return a failed result.

We return to the statmeent multiparsing method and try to parse the next statement.

The `1` token is encountered, it doesn't correspond to any `statementParsers`. This causes an error with an empty `SourceTokens` so simply read 1 token.

We encounter equivalent errors for the `=`, `0`, `alors`, and finally `finsi` so they are not shown.

Finally we see `fin` which marks the end of our `MainProgram`.

```text
[P0002] L 3, col 5: error: expected `(` before integer literal `1`
    2 |     si 1 = 0 alors
      |     ^^^^
[P0002] L 3, col 8: error: expected statement before integer literal `1`
    2 |     si 1 = 0 alors
      |        ^
```

Now let's see what GCC outputs for an equivalent C program :

```c
int main() {
    if 1 == 0 {

    }
}
```

```text
test.c: In function ‘main’:
test.c:2:8: error: expected ‘(’ before numeric constant
    2 |     if 1 == 0 {
      |        ^
      |        (
```

There's only one error.

What we could do is simply :

1. When we fail due to the missing open bracket, register the failure (set a boolean in `ParseOperation`) but continue parsing as usual (return null and do not read the token so it can be parsed in the next step, but don't switch to the failure implementation)
2. When eventually we fail again due to encountering `alors` instead of the closing bracket `)`, apply the same strategy.
3. We should be able to finish parsing the if statement and we will get no superflous errors.

The question is, could this approach hide useful errors?

Also do we still need **Solution 4** if we implement this?

No i don't think so. If we take our previous example:

```text
programme HelloWorld c'est
début
    écrireEcran("")
    écrireEcran("")
    écrireEcran("")
    écrireEcran("")
fin
```

When we miss a semicolon we will simply ignore it (but still register the error).

So we will get 4 errors and not more since we won't be reading the next `écrireEcran`.

An adjustment that we should make though is keeping a list of the errors that occured in the `ParseOperation`. Since now that we don't stop on the first error we can have multiple errors.

## identifier and word primitive obession

Create a `Word` class that abstracts a string but with restrictions (`\w+`)

The point would be to prevent invalid identifiers. But we already check in the tokenizer. So what's the point?

Semantically, it would able us to tell if an indentifier is expected instead of any string.

This could be useful for string constants.

## Create a representation of the AST being built

We know the ast is build from the bottom up, but it could be interesting to see it animated.

Simply add some graph-building logic in the NodeImpl constructor.

## Lvalue/rvalue

We need to differenciate between lvalues and rvalues in the formal grammar, the ast and the parser.

So we don't allow things like `lireClavier(69)`

Also we need to change our FG with assigment, currently it only allows identifiers as the left operands :

`target := value`

However arrays exist:

`array[5] := value`

`array[indice / 2 + 4] := value`

---

I can no longer postpone this

Representing Rvalues with Node.Expressions and Lvalues with strings is no longer enough as structure component access expressions may be used as Lvalues.

Should we update our formal grammar?

Yes. We need some sort of restriction on which expressions can be lvalues so we don't allow horrors such as

`1 + 1 := 3;`

```cs
internal interface LValue : Expression
{
    
}
```

## Fix ParseOperation multiparsing logic

We need to cleanup the logic for multiparsing.

We have two multiplicities :

1. \* : zero or more
2. \+ : one or more

We have 2 strategies :

2. Parse until a token type is encountered
3. Parse separated by a token type.

## Better TokenType names

Currently we end up showing the type name which is dirty.

```cs
internal sealed class TokenType
{
    private TokenType(string displayName) => DisplayName = displayName;

    public string DisplayName { get; }

    public static TokenTypeNew OpenBracket { get; } = new("(");
}
```
