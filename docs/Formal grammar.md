# Formal grammar

A production rule is defined as `⟨RuleName⟩ \to ...` (`...` means zero or more items).

item|apperance|description
-|-|-
`⟨NonTerminal⟩`|$⟨NonTerminal⟩$|reference to or definition of a production rule. Name matches `[A-Za-z0-9]+`.
`⟨NonTerminal⟩ \to ...`|$⟨NonTerminal⟩ \to ...$|Inline production definition. Name matches `[A-Za-z0-9]+`.
`terminal`|$terminal$|reference to a named terminal. Name matches `[A-Za-z0-9_]+`.
`\text{bare\ text}`|$\text{bare text}$|exact string (unnamed terminal)
`\begin{pmatrix}...\end{pmatrix}`|$\begin{pmatrix}...\end{pmatrix}$|grouping
`\begin{Bmatrix*}[l]......\\...\end{Bmatrix*}`|$\begin{Bmatrix*}[l]......\\...\end{Bmatrix*}$|alternative. choices are separated by newlines (`\\`). A trailing newline on the last choice is useless but allowed.
`\begin{split}...\\...\end{split}`|$$\begin{split}...\\...\end{split}$$|split a long line for readability. Represents its contents, akin to a grouping.

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

---

$$
\begin {align*}

&\textbf{General}\\

&⟨Algorithm⟩ \to \text{programme}\ Identifier\ \text{c'est} ⟨Declaration⟩^*
\\
&⟨Block⟩ \to ⟨Statement⟩^*

\\\\&\textbf{Declarations}\\

&⟨Declaration⟩ \to \begin{Bmatrix*}[l]
    ⟨DeclarationTypeAlias⟩ \to \text{type}\ Identifier = ⟨Type⟩ \\
    ⟨DeclarationCompleteTypeAlias⟩ \to \text{type}\ Identifier = ⟨TypeComplete⟩ \\
    ⟨DeclarationConstant⟩ \to \text{constante} ⟨TypeComplete⟩ Identifier \text{:=} ⟨Expr⟩\text{;} \\
    ⟨MainProgram⟩ \to \text{début} ⟨Block⟩ \text{fin} \\
    ⟨FunctionDeclaration⟩ \to ⟨FunctionSignature⟩ \text{;} \\
    ⟨FunctionDefinition⟩ \to ⟨FunctionSignature⟩ \text{c'est\ début} ⟨Block⟩ \text{fin} \\
    ⟨ProcedureDeclaration⟩ \to ⟨Procedureignature⟩ \text{;} \\
    ⟨ProcedureDefinition⟩ \to ⟨ProcedureSignature⟩ \text{c'est\ début} ⟨Block⟩ \text{fin} \\
    \text{début} ⟨Block⟩ \text{fin} \\
\end{Bmatrix*}

\\&\textbf{Statements}\\

&⟨Statement⟩ \to \begin{Bmatrix*}[l]
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
    &   \begin{Bmatrix*}[l]
            \text{pour}\ Identifier\ \text{de} ⟨Expr⟩ \text{à} ⟨Expr⟩ \begin{pmatrix}\text{pas} ⟨Expr⟩\end{pmatrix}^?\\
            \text{pour}(Identifier\ \text{de} ⟨Expr⟩ \text{à} ⟨Expr⟩ \begin{pmatrix}\text{pas} ⟨Expr⟩\end{pmatrix}^?)\\
        \end{Bmatrix*}
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
    ⟨LocalVariable⟩ \to ⟨NameTypeBinding⟩\text{:=}⟨Initializer⟩\text{;}
    \\
    ⟨Return⟩ \to \text{retourne} ⟨Expr⟩\text{;}
    \\
    ⟨BuiltinAssigner⟩ \to \text{assigner}(⟨Lvalue⟩,⟨Expr⟩)\text{;}
    \\
    ⟨BuiltinEcrire⟩ \to \text{écrire}(⟨Expr⟩, ⟨Expr⟩)\text{;}
    \\
    ⟨BuiltinEcrireEcran⟩ \to \text{écrireÉcran}(⟨Expr⟩^{*\#})\text{;}
    \\
    ⟨BuiltinFermer⟩ \to \text{fermer}(⟨Expr⟩)\text{;}
    \\
    ⟨BuiltinLire⟩ \to \text{lire}(⟨Expr⟩,⟨Lvalue⟩)\text{;}
    \\
    ⟨BuiltinLireClavier⟩ \to \text{lireClavier}(⟨Lvalue⟩)\text{;}
    \\
    ⟨BuiltinOuvrirAjout⟩ \to \text{ouvrirAjout}(⟨Expr⟩)\text{;}
    \\
    ⟨BuiltinOuvrirEcriture⟩ \to \text{ouvrirÉcriture}(⟨Expr⟩)\text{;}
    \\
    ⟨BuiltinOuvrirLecture⟩ \to \text{ouvrirLecture}(⟨Expr⟩)\text{;}
    \\
    ⟨Nop⟩ \to \text{;}
\end{Bmatrix*}
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
\\
&⟨NameTypeBinding⟩ \to Identifier^{+\#} : ⟨TypeComplete⟩
\\
&⟨Initializer⟩ \to \begin{Bmatrix*}[l]
    ⟨Expr⟩
    \\
    ⟨StructureInitializer⟩ \to \text{\{}
        \begin{pmatrix}
            \begin{pmatrix}
                ⟨Component⟩
                \text{=}
            \end{pmatrix}^?
            ⟨Initializer⟩
        \end{pmatrix}^{*\#}
    \text{,}^? \text{\}}
    \\
    ⟨ArrayInitializer⟩ \to \text{\{}
        \begin{pmatrix}
            \begin{pmatrix}
                ⟨Index⟩
                \text{=}
            \end{pmatrix}^?
            ⟨Initializer⟩
        \end{pmatrix}^{*\#}
    \text{,}^? \text{\}}
\end{Bmatrix*}
\\
&⟨Component⟩ \to \text{.}Identifier
\\
&⟨Index⟩ \to \text{[}⟨Expr⟩^{+\#}\text{]}

\\\\&\textbf{Exprs}\\

&⟨Expr⟩ \to ⟨Expr_{Or}⟩
\\
&⟨Expr_{Or}⟩ \to ⟨Expr_{And}⟩\begin{pmatrix}
\text{OU}
⟨Expr_{And}⟩\end{pmatrix}^*
\\
&⟨Expr_{And}⟩ \to ⟨Expr_{Xor}⟩\begin{pmatrix}
\text{ET}
⟨Expr_{Xor}⟩\end{pmatrix}^*
\\
&⟨Expr_{Xor}⟩ \to ⟨Expr_{Equality}⟩\begin{pmatrix}
\text{XOR}
⟨Expr_{Equality}⟩\end{pmatrix}^*
\\
&⟨Expr_{Equality}⟩ \to ⟨Expr_{Comparison}⟩\begin{pmatrix}
\begin{Bmatrix*}[l]
    \text{!=}\\
    \text{==}\\
\end{Bmatrix*}
⟨Expr_{Comparison}⟩\end{pmatrix}^*
\\
&⟨Expr_{Comparison}⟩ \to ⟨Expr_{AddSub}⟩\begin{pmatrix}
\begin{Bmatrix*}[l]
    \text{<}\\
    \text{<=}\\
    \text{>}\\
    \text{>=}\\
\end{Bmatrix*}
⟨Expr_{AddSub}⟩\end{pmatrix}^*
\\
&⟨Expr_{AddSub}⟩ \to ⟨Expr_{MulDivMod}⟩\begin{pmatrix}
\begin{Bmatrix*}[l]
    \text{-}\\
    \text{+}\\
\end{Bmatrix*}
⟨Expr_{MulDivMod}⟩\end{pmatrix}^*
\\
&⟨Expr_{MulDivMod}⟩ \to ⟨Expr_{Unary}⟩\begin{pmatrix}
\begin{Bmatrix*}[l]
    \text{*}\\
    \text{/}\\
    \text{\%}\\
\end{Bmatrix*}
⟨Expr_{Unary}⟩\end{pmatrix}^*
\\
&⟨Expr_{Unary}⟩ \to \begin{Bmatrix*}[l]
    \begin{Bmatrix*}[l]
        \text{-}\\
        \text{+}\\
        \text{NON}\\
    \end{Bmatrix*} ⟨Expr_{Unary}⟩\\
    ⟨Expr_{Primary}⟩\\
\end{Bmatrix*}
\\
&⟨Expr_{Primary}⟩ \to \begin{Bmatrix*}[l]
    ⟨Lvalue⟩\\
    ⟨TerminalRvalue⟩\\
\end{Bmatrix*}
\\
&⟨Lvalue⟩ \to \begin{Bmatrix*}[l]
    ⟨ArraySubscript⟩\\
    ⟨ComponentAccess⟩\\
    ⟨TerminalLvalue⟩\\
\end{Bmatrix*}
\\
&⟨ArraySubscript⟩ \to ⟨TerminalRvalue⟩⟨Index⟩^+
\\
&⟨ComponentAccess⟩ \to ⟨TerminalRvalue⟩⟨Component⟩^+
\\
&⟨TerminalLvalue⟩ \to \begin{Bmatrix*}[l]
    ⟨BracketedLvalue⟩ \to (⟨Lvalue⟩)
    \\
    ⟨IdentifierReference⟩ \to Identifier
\end{Bmatrix*}
\\
&⟨TerminalRvalue⟩ \to \begin{Bmatrix*}[l]
    ⟨Bracketed⟩ \to (⟨Expr⟩)
    \\
    ⟨Literal⟩ \to \begin{Bmatrix*}[l]
        LiteralCharacter\\
        LiteralInteger\\
        LiteralReal\\
        LiteralString\\
        \text{vrai}\\
        \text{faux}\\
    \end{Bmatrix*}
    \\
    ⟨Call⟩
    \\
    ⟨BuiltinFdf⟩ \to \text{FdF}(⟨Expr⟩)
    \\
    ⟨TerminalLvalue⟩
\end{Bmatrix*}
\\
&⟨Call⟩ \to Identifier(⟨ParameterActual⟩^{*\#})

\\\\&\textbf{Types}\\

&⟨Type⟩ \to \begin{Bmatrix*}[l]
    ⟨TypeComplete⟩
    \\
    ⟨TypeAliasReference⟩ \to Identifier
    \\
&    ⟨String⟩ \to \text{chaîne}
\end{Bmatrix*}
\\
&⟨TypeComplete⟩ \to \begin{Bmatrix*}[l]
    ⟨TypeCompleteAliasReference⟩ \to Identifier
    \\
    ⟨TypeNumeric⟩ \to \begin{Bmatrix*}[l]
        \text{booléen}\\
        \text{caractère}\\
        \text{entier}\\
        \text{réel}\\
    \end{Bmatrix*}
    \\
    ⟨File⟩ \to \text{nomFichierLog}
    \\
    ⟨StringLengthed⟩ \to \text{chaîne}(⟨Expr⟩)
    \\
    ⟨Structure⟩ \to \text{structure\ début} \begin{pmatrix}⟨NameTypeBinding⟩\text{;}\end{pmatrix}^+ \text{fin}
    \\
    ⟨TypeArray⟩ \to \text{tableau} {\text{[}⟨Expr⟩\text{]}}^+ \text{de} ⟨TypeComplete⟩
\end{Bmatrix*}

\\\\&\textbf{Other}\\

&⟨ParameterFormal⟩ \to \begin{Bmatrix*}[l]
    \text{entF}\\
    \text{sortF}\\
    \text{entF/sortF}\\
\end{Bmatrix*} Identifier\text{:}⟨Type⟩
\\
&⟨ParameterActual⟩ \to \begin{Bmatrix*}[l]
    \text{entE}\\
    \text{sortE}\\
    \text{entE/sortE}\\
\end{Bmatrix*} ⟨Expr⟩\
\\
&⟨FunctionSignature⟩ \to \text{fonction}\ Identifier(⟨ParameterFormal⟩^{*\#}) \text{délivre} ⟨Type⟩
\\
&⟨ProcedureSignature⟩ \to \text{procédure}\ Identifier(⟨ParameterFormal⟩^{*\#})

\end{align*}
$$
