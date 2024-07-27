# LaTaX

LaTeX-based meta-language.

A production rule is defined as `⟨RuleName⟩ \to ...` (`...` means zero or more items).

item|apperance|description
-|-|-
`⟨NonTerminal⟩`|$⟨NonTerminal⟩$|reference to or definition of a production rule.
`⟨NonTerminal⟩ \to ...`|$⟨NonTerminal⟩ \to ...$|production definition.
`terminal`|$terminal$|reference to a named terminal. Name matches `[A-Za-z0-9_]+`.
`\text{bare\ text}`|$\text{bare text}$|exact string (unnamed terminal)
`\begin{pmatrix}...\end{pmatrix}`|$\begin{pmatrix}...\end{pmatrix}$|grouping
`\begin{Bmatrix*}[l]......\\...\\\end{Bmatrix*}`|$\begin{Bmatrix*}[l]......\\...\\\end{Bmatrix*}$|alternative. choices are separated by newlines (`\\`)..
`\begin{split}...\\...\\\end{split}`|$$\begin{split}...\\...\\\end{split}$$|split a long line for readability. Represents its contents, akin to a grouping.

Note : the Epsilon (&epsilon;) special rule can be matched with the empty grouping `{}`.

Modifiers can be applied to items using the syntax : `[item]^[modifier]` or `[item]^{[modifier]}` if `[modifier]` is more than 1 character log.

modfier|appearance|description
-|-|-
`*`|$^*$|zero or more (repetition)
`+`|$^+$|one or more
`?`|$^?$|zero or one (optional)
`{*\#}`|$^{*\#}$|zero or more, separated by commas (trailing comma not allowed)
`{+\#}`|$^{+\#}$|one or more, separated by commas (trailing comma not allowed)

Whitespace in the metasyntax and in the defined language is insignificant and cannot be matched.
