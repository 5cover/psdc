# Scratch area

```cs
private static ParseResult<Node.Declaration.ProcedureDeclaration> ParseProcedureDeclaration(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(TokenType.KeywordProcedure)
        .ParseToken(TokenType.Identifier, out var name)
        .ParseToken(TokenType.OpenBracket)
        .ParseToken(TokenType.CloseBracket)
```

## Handle inner errors

So basically when an error happens inside of the main program, we continue to parse Token by Token and fail until we consume everything up to `TokenType.Eof`. That's incorrect, as we'll end up consuming `TokenType.KeywordEnd`.

This is sorta linked with "Errors that go token after token".

What can we do?

Solution 1 : stop oneormore and zeroormore when item parsing fails

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

**Solution 4** : skim until first token of the target node, then resume parsing

> this could actually work

## Lvalue/rvalue

We need to differenciate between lvalues and rvalues in the formal grammar, the ast and the parser.

So we don't allow things like `lireClavier(69)`

Also we need to change our FG with assigment, currently it only allows identifiers as the left operands :

`target := value`

However arrays exist:

`array[5] := value`

`array[indice / 2 + 4] := value`

## Simplification

The code has become a big mess because we've made the wrong choices.

First of all, it's not the code generator role's to generate syntax errors. It would be the duty of the parser.

Second of all, the way we handle failure is all wrong. Having everyting wrapped in `ParseResult`s results in tons of boilerplate code.

So I propose ge get rid of `ParseResult` entirely.

So how do we handle failure now?

Well let's take an example of what a failure may look like:

```text
retourne result
```

We've forgotten the terminating semicolon in that return statement.

What we do now is we add a failed return statement to the AST, and the code generator registers a syntax error for it when it tries to generate it.

There's not need to have the code generator involved here at all.

Just account for the failure, add an error message, and move on.

## Multiparsing madness

Stop failing and retrying with a diferent parser, and combinding the erros that occur, all based on the first token which is parsed twice.

We're parsing things multiple times and it makes no sense.

Let's build a tree so that the common parts of parsers can be shared.

We'll need a way to branch in the parser

Start with assignements and variable declarations.
