# Scratch area

## Errors that go token after token

fix errors going one from one token:

so basically when a parsing error occurs in a parsing loop, what we currently do is move to the next token and resume parsing from there.

So whenever an error occurs, get a bunch of other errors for the following tokens, even though they were valid.
If ecrireEcran is mispelled, we get "expected ecrireEcran" for all tokens up to the final semi.

Solution 1 : when error, keep reading and silence further errors until we hit something valid or lack of tokens

> this doesn't work because we don't know when do stop reading invalid stuff. We could hit on a potentially next invalid statmement.

Solution 2 : ParseError should have a property for the minimal amount of expected tokens.
Then we can just skip that amount of tokens next time we parse.

> tedious and prolly unreliable

Solution 3 : who cares, it's not that big of a deal.

> it clutters the message list

Solution 4 : skim until first token of the target node, then resume parsing

> this could actually work

Solution 5 : skim until valid or failure with different error

> Require comparison of ParseError, could work as well. Could result in missing errors.

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
