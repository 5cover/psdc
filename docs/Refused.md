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

## Change `:=` to `=` in initialization

**Reason**: consistency with official rules

`:=` means assignment. Initialization isn't assignment since we're setting the value for the first time. Although you could have the same argument for constants, constants use `:=` for the value officially, so maybe it is logical to follow the same logic.

## Simplifing initializer items

**Reason**: another solution found

This is getting quite complicated. I wonder, could we have a process of "simplification" for the initializers? So an initializers simplfies down to all values having exactly one designator. Then we can just reuse our current code.

Consider in C:

```c
struct {int sec,min,hour,day,mon,year;} z = {.day=31,12,2014,.sec=30,15,17}; // initializes z to {30,15,17,31,12,2014}
```

Simplifies to:

```c
struct {int sec,min,hour,day,mon,year;} z = {
    .day=31,
    .mon=12,
    .year=2014,
    .sec=30,
    .min=15,
    .hour=17
};
```

Nested initializer:

```c
struct example {
    struct addr_t {
       uint32_t port;
    } addr;
    union {
       uint8_t a8[4];
       uint16_t a16[2];
    } in_u;
};
struct example ex = {
    { 80 },
    { {127,0,0,1} }
};
```

Simplfies to:

```c
struct example ex = {
    .addr = { .port = 80 },
    .in_u = { .a8 = {
            [0]=127,
            [1]=0,
            [2]=0,
            [3]=1
    } }
};
```

Using nested designators:

```c
struct example ex2 = {
    .in_u.a8[0]=127,
    0,
    0,
    1,
    .addr=80
};
```

```c
struct example ex2 = {
    .in_u = { .a8 = {
        [0]=127,
        [1]=0,
        [2]=0,
        [3]=1
    } },
    .addr = 80
}
```

More:

```c
struct example ex3 = {
    80,
    .in_u = {
        127,
        .a8[2 ] =1
    }
};
```

```c
struct example ex3 = {
    .addr = 80,
    .in_u = { .a8 = {
        [0] = 127,
        [1] = 0,
        [2] = 0,
        [3] = 1
    } }
}
```

- Expand successive designators into brace pairs.
- Give undesignated values a designator based on natural order.

No need to add unmentioned values, since we'll start from the default value for the haystack.

This shoulbe easy enough, provided that:

```c
struct { int v[3]; }
    x = { .v = { [1] = 5 } }, // Zeroes [0] and [2]
    y = { .v[1] = 5 }; // Does it too or does it leave them uninitialized?
// Are equivalent.
```

That's the case. Let's go.

The simplified items are held in a structure private to StaticAnalyzer. (since semantic node must keep all info in order to keep the same syntaw when genereating C)

SimpleBracedInitializerItem

- DesignatorInfo Designator
- Value Value

Implementation

- Produce the list of simplified items along with the list of semantic items in the AnalyzeItems lambda

After the AnalyzeItems call, foreach the simplified items and update the value at the designator. (use a SetValue method on designator info, ig)

To simplify an item:

- Advance the natural order
- Convert multiple designators into nested braced initializers.

What about the SourceTokens of the node we produce?

Sratch that.
