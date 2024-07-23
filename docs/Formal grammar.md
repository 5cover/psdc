# Formal grammar

A production rule is defined as `⟨RuleName⟩ \to ...` (`...` means zero or more items).

item|apperance|description
-|-|-
`⟨NonTerminal⟩`|$⟨NonTerminal⟩$|reference to or definition of a production rule. Name matches `[A-Za-z0-9]+`.
`⟨NonTerminal⟩ \to ...`|$⟨NonTerminal⟩ \to ...$|Inline production definition. Name matches `[A-Za-z0-9]+`.
`terminal`|$terminal$|reference to a named terminal. Name matches `[A-Za-z0-9_]+`.
`\text{bare\ text}`|$\text{bare text}$|exact string (unnamed terminal)
`\{...\}`|$...$|grouping
`\begin{cases}...\\...\end{cases}`|$\begin{cases}...\\...\end{cases}$|alternative. choices are separated by newlines (`\\`). A trailing newline on the last choice is useless but allowed.
`\begin{split}...\\...\end{split}`|$$\begin{split}...\\...\end{split}$$|split a long line for readability. Represents its contents, akin to a grouping.

Note : the Epsilon (&epsilon;) special rule can be matched with the empty grouping `{}`.

Modifiers can be applied to items using the syntax : `[item]^[modifier]` or `[item]^{[modifier]}` if `[modifier]` is more than 1 character log.

modfier|appearance|description
-|-|-
`*`|$^*$|zero or more (repetition)
`+`|$^+$|one or more
`?`|$^?$|zero or one (optional)
`*\|[item]`|$^{*\|item}$|zero or more separated
`+\|[item]`|$^{+\|item}$|one or more separated

Whitespace in the metasyntax and in the defined language is insignificant and cannot be matched.

---

$$
\begin {align*}

&\textbf{General}\\

&⟨Algorithm⟩ \to \text{programme}\ Identifier\ \text{c'est} ⟨Declaration⟩^*
\\
&⟨Block⟩ \to ⟨Statement⟩^*

\\&\textbf{Declarations}\\

&⟨Declaration⟩ \to \begin{cases}
    ⟨DeclarationTypeAlias⟩ \to \text{type}\ Identifier = ⟨Type⟩
    \\
    ⟨DeclarationCompleteTypeAlias⟩ \to \text{type}\ Identifier = ⟨TypeComplete⟩
    \\
    ⟨DeclarationConstant⟩ \to \text{constante} ⟨TypeComplete⟩ Identifier \text{:=} ⟨Expr⟩\text{;}
    \\
    ⟨MainProgram⟩ \to \text{début} ⟨Block⟩ \text{fin}
    \\
    ⟨FunctionSignature⟩ \to  \text{c'est\ début} ⟨Block⟩ \text{fin}
    \\
    ⟨FunctionSignature⟩ \to \text{;}
    \\
    ⟨ProcedureSignature⟩ \to  \text{c'est\ début} ⟨Block⟩ \text{fin}
    \\
    ⟨ProcedureSignature⟩ \to \text{;}
    \\
    \text{début} ⟨Block⟩ \text{fin}
    \\
\end{cases}

\\&\textbf{Statements}\\

&⟨Statement⟩ \to \begin{cases}
    ⟨Alternative⟩ \to \begin{split}
    &   ⟨Alternative.If⟩\\
    &   ⟨Alternative.ElseIf⟩^*\\
    &   ⟨Alternative.Else⟩^?\\
    &   \text{finsi}\\
    \end{split}
    \\
    ⟨ProcedureCall⟩ \to ⟨Call⟩\text{;}
    \\
    ⟨Assignment⟩ \to ⟨Lvalue⟩ = ⟨Expr⟩\text{;}
    \\
    ⟨ForLoop⟩ \to \begin{split}
    \\
    &   \begin{cases}
            \text{pour}\ Identifier\ \text{de} ⟨Expr⟩ \text{à} ⟨Expr⟩ \{\text{pas} ⟨Expr⟩\}^?\\
            \text{pour}(Identifier\ \text{de} ⟨Expr⟩ \text{à} ⟨Expr⟩ \{\text{pas} ⟨Expr⟩\}^?)\\
        \end{cases}
        \text{faire} \\
    &   ⟨Block⟩ \\
    &   \text{finfaire}
    \end{split}
    \\
    ⟨RepeatLoop⟩ \to \text{répéter} ⟨Block⟩ \text{jusqu'à} ⟨Expr⟩
    \\
    ⟨DoWhileLoop⟩ \to \text{faire} ⟨Block⟩ \text{tant\ que} ⟨Expr⟩
    \\
    ⟨WhileLoop⟩ \to \text{tant\ que} ⟨Expr⟩ \text{faire} ⟨Block⟩ \text{finfaire}
    \\
    ⟨Switch⟩ \to \begin{split}
    &   \text{selon} ⟨Expr⟩ \text{c'est}\\
    &   ⟨Switch.Case⟩^*\\
    &   ⟨Switch.Default⟩^?\\
    &   \text{finselon}\\
    \end{split}
    \\
    ⟨LocalVariable⟩ \to Identifier^{+|\text{,}} : ⟨TypeComplete⟩\text{;}
    \\
    ⟨Return⟩ \to \text{retourne} ⟨Expr⟩\text{;}
    \\
    ⟨BuiltinAssigner⟩ \to \text{assigner}(⟨Lvalue⟩,⟨Expr⟩)\text{;}
    \\
    ⟨BuiltinEcrire⟩ \to \text{écrire}(⟨Expr⟩, ⟨Expr⟩)\text{;}
    \\
    ⟨BuiltinEcrireEcran⟩ \to \text{écrireÉcran}(⟨Expr⟩^{*|\text{,}})\text{;}
    \\
    ⟨BuiltinFermer⟩ \to \text{fermer}(⟨Expr⟩)\text{;}
    \\
    ⟨BuiltinLire⟩ \to \text{lire}(⟨Expr⟩,⟨Lvalue⟩)\text{;}
    \\
    ⟨BuiltinLireClavier⟩ \to \text{lireClavier}(⟨Lvalue⟩)\text{;}
    \\
    ⟨BuiltinOuvrirAjout⟩ \to \text{ouvrirAjout}(⟨Expr⟩)
    \\
    ⟨BuiltinOuvrirEcriture⟩ \to \text{ouvrirÉcriture}(⟨Expr⟩)\text{;}
    \\
    ⟨BuiltinOuvrirLecture⟩ \to \text{ouvrirLecture}(⟨Expr⟩)\text{;}
    \\
    ⟨Nop⟩ \to \text{;}
\end{cases}
\\
&⟨Alternative.If⟩ \to \text{si} ⟨Expr⟩ \text{alors} ⟨Block⟩
\\
&⟨Alternative.ElseIf⟩ \to \text{sinonsi} ⟨Expr⟩ \text{alors} ⟨Block⟩\
\\
&⟨Alternative.Else⟩ \to \text{sinon} ⟨Block⟩
\\
&⟨Switch.Case⟩ \to \text{quand} ⟨Expr⟩ => ⟨Statement⟩^+\
\\
&⟨Switch.Default⟩ \to \text{quand\ autre} => ⟨Statement⟩^+\

\\&\textbf{Exprs}\\

&⟨Expr⟩ \to ⟨Expr_{Or}⟩
\\
&⟨Expr_{Or}⟩ \to ⟨Expr_{And}⟩\{
\text{OU}
⟨Expr_{And}⟩\}^*
\\
&⟨Expr_{And}⟩ \to ⟨Expr_{Xor}⟩\{
\text{ET}
⟨Expr_{Xor}⟩\}^*
\\
&⟨Expr_{Xor}⟩ \to ⟨Expr_{Equality}⟩\{
\text{XOR}
⟨Expr_{Equality}⟩\}^*
\\
&⟨Expr_{Equality}⟩ \to ⟨Expr_{Comparison}⟩\{
\begin{cases}
    \text{!=}\\
    \text{==}\\
\end{cases}
⟨Expr_{Comparison}⟩\}^*
\\
&⟨Expr_{Comparison}⟩ \to ⟨Expr_{AddSub}⟩\{
\begin{cases}
    \text{<}\\
    \text{<=}\\
    \text{>}\\
    \text{>=}\\
\end{cases}
⟨Expr_{AddSub}⟩\}^*
\\
&⟨Expr_{AddSub}⟩ \to ⟨Expr_{MulDivMod}⟩\{
\begin{cases}
    \text{-}\\
    \text{+}\\
\end{cases}
⟨Expr_{MulDivMod}⟩\}^*
\\
&⟨Expr_{MulDivMod}⟩ \to ⟨Expr_{Unary}⟩\{
\begin{cases}
    \text{*}\\
    \text{/}\\
    \text{\%}\\
\end{cases}
⟨Expr_{Unary}⟩\}^*
\\
&⟨Expr_{Unary}⟩ \to \begin{cases}
    \begin{cases}
        \text{-}\\
        \text{+}\\
        \text{NON}\\
    \end{cases} ⟨Expr_{Unary}⟩\\
    ⟨Expr_{Primary}⟩\\
\end{cases}
\\
&⟨Expr_{Primary}⟩ \to \begin{cases}
    ⟨Lvalue⟩\\
    ⟨TerminalRvalue⟩\\
\end{cases}
\\
&⟨Lvalue⟩ \to \begin{cases}
    ⟨ArraySubscript⟩\\
    ⟨ComponentAccess⟩\\
    ⟨TerminalLvalue⟩\\
\end{cases}
\\
&⟨ArraySubscript⟩ \to ⟨TerminalRvalue⟩\{\text{[}⟨Expr⟩^{+|\text{,}}\text{]}\}^+
\\
&⟨ComponentAccess⟩ \to ⟨TerminalRvalue⟩\{\text{.}⟨Identifier⟩\}^+
\\
&⟨TerminalLvalue⟩ \to \begin{cases}
    ⟨BracketedLvalue⟩ \to (⟨Lvalue⟩)
    \\
    ⟨IdentifierReference⟩ \to Identifier
\end{cases}
\\
&⟨TerminalRvalue⟩ \to \begin{cases}
    ⟨Bracketed⟩ \to (⟨Expr⟩)
    \\
    ⟨Literal⟩ \to \begin{cases}
        LiteralString\\
        LiteralCharacter\\
        LiteralInteger\\
        LiteralReal\\
        \text{vrai}\\
        \text{faux}\\
    \end{cases}
    \\
    ⟨Call⟩
    \\
    ⟨BuiltinFdf⟩ \to \text{FdF}(⟨Expr⟩)
    \\
    ⟨TerminalLvalue⟩
\end{cases}
\\
&⟨Call⟩ \to Identifier(⟨ParameterActual⟩^{*|\text{,}})

\\&\textbf{Types}\\

&⟨Type⟩ \to \begin{cases}
    ⟨TypeComplete⟩
    \\
    ⟨TypeAliasReference⟩ \to Identifier
    \\
&    ⟨String⟩ \to \text{chaîne}
\end{cases}
\\
&⟨TypeComplete⟩ \to \begin{cases}
    ⟨TypeCompleteAliasReference⟩ \to Identifier
    \\
    ⟨TypeNumeric⟩ \to \begin{cases}
        \text{booléen}\\
        \text{caractère}\\
        \text{entier}\\
        \text{réel}\\
    \end{cases}
    \\
    ⟨File⟩ \to \text{nomFichierLog}
    \\
    ⟨StringLengthed⟩ \to \text{chaîne}(⟨Expr⟩)
    \\
    ⟨Structure⟩ \to \text{structure\ début} ⟨LocalVariable⟩^+ \text{fin}
    \\
    ⟨TypeArray⟩ \to \text{tableau} {\text{[}⟨Expr⟩\text{]}}^+ \text{de} ⟨TypeComplete⟩
\end{cases}

\\&\textbf{Other}\\

&⟨ParameterFormal⟩ \to \begin{cases}
    \text{entF}\\
    \text{sortF}\\
    \text{entF/sortF}\\
\end{cases} Identifier\text{:}⟨Type⟩
\\
&⟨ParameterActual⟩ \to \begin{cases}
    \text{entE}\\
    \text{sortE}\\
    \text{entE/sortE}\\
\end{cases} ⟨Expr⟩\
\\
&⟨FunctionSignature⟩ \to \text{fonction}\ Identifier(⟨ParameterFormal⟩^{*|\text{,}}) \text{délivre} ⟨Type⟩
\\
&⟨ProcedureSignature⟩ \to \text{procédure}\ Identifier(⟨ParameterFormal⟩^{*|\text{,}})

\end{align*}
$$
