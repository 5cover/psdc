# Refused

Ideas that aren't planned be added or have been changed. Still keep them around as a reminder of why we did not add that.

## Semantic preprocessor directives

**Reason**: it is better to have a preprocessor syntax `#` that is interpreted before parsing. Compiler directives require static analysis and are a separate concept. Trying to use the same syntax for both would have been confusing. It's fine if they are not allowed everywhere, too. Top-level, statement and struct declarations is all we really need. They should feel like an instruction, like `static_assert` in C.

Instead of using compiler directives, keep the `#` syntax.

## Freestanding tokens for directives

- Parse them as compiler directives (CDs) using a separate token chanel and a new parser class
- Somehow able them to be evaluated in the SA, in the context of regular AST nodes. I see solutions for this:
    - Somehow put CDs in the regular AST: nope
    - Put them in a list that is passed alongside the AST (no need for a tree since they are sequential). Compare the source tokens of each AST node to progress gradually through the list of CDs, and evaluate them when we move down one: nope
    - Same as above, but somehow keep a context so we don't have to compare the SourceTokens of every analyzed AST node to locate the CD: ok
        - Maybe keep a reference, in the CD, to the last non-CD token parsed. Then if the SourceTokens of the node contain this token:
            - if this token is the last token of the node, then it the CD comes after the node, so evaluate it after the node.
            - otherwise, it is inside the node, so evaluate it before

Having to do all these checks in the SA is kind of making things complex. We should simplify this.

Maybe we add a Children IEnumerable to Node that is implemented as the direct children of each node? Or an empty enumerable if this node has no children.

Then, we can enumerate on that property and recursively find the smallest node that contains the directive.

But who cares about the small nodes that we're probably not even gonna evaluate.

### New idea: no more tokens channel, ignore directives in ParseOperation

Tokenize in the same way as before.

In the methods, ParseOperation, explicitly ignore Directive tokens. (use a static IgnoredTokenTypes list), but still account for them in read counts.

Now, the SourceTokens of each node may or may not contain Directives.

This means that we can just check for them in the SA, no additional logic needed. As earlier, if the directive is last sourcetoken of the node, evaluate them after instead of before.

Don't forget to account for the fact that there may be multiple directives in the SourceTokens of one node.

I like this. To make this easier, require using helper methods in ParseOperation to acess tokens. That way we don't have to remember performing this check everywhere.

Scratch that. The static analysis becomes too convoluted. **Allowing stuff anywhere is just a bad idea.**

### Go back to simplicity

I'm not gonna reuse C's syntax. The @ syntax is better, because it does not incur significant whitespace and does not require a line continuator: '\' to have multiline stuff

```text
@assert(1 + 1 == 2)
@error("failed existing")
@warn("adult life is hard")
@info("i have no friends")
@debug(1 + 1)

// conditional compilation
@if vrai {

@ } else {

@ }
```

We consider lines that begin with '@' (possibly with leading whitespace), to be **directive lines**. Requiring this clearly indicates that they belong to a sub-language.
