# Formal grammar

symbol|legend
-|-
⟨PascalCalse⟩|non-terminal (reference to other crule)
camelCase|terminal (token)
*camelCase*|terminal (valued token)
\*|zero or more
\+|one or more
\?|optional
\|x|separated by *x*

|typography
|-
|`⟨Non\ terminal⟩`
|`terminal`
|`\text{bare\ text}`
|`;` (bare symbols are undecorated)

---

$$
\begin{align*}

&\textbf{Général}\notag\\

⟨Algorithm⟩ &\to \text{programme}\ Identifier\ \text{c'est} ⟨Declaration⟩^*
\\
⟨Block⟩ &\to ⟨Statement⟩^*

\\&\textbf{Declarations}\notag\\

⟨Declaration⟩ &\to \begin{cases}
    ⟨DeclarationTypeAlias⟩ \to \text{type}\ Identifier = ⟨Type⟩
    ⟨DeclarationCompleteTypeAlias⟩ \to \text{type}\ Identifier = ⟨TypeComplete⟩
    \\
    ⟨DeclarationConstant⟩ \to \text{constante} ⟨TypeComplete⟩ Identifier \text{:=} ⟨Expression⟩;
    \\
    ⟨MainProgram⟩ \to \text{début} ⟨Block⟩ \text{fin}
    \\
    ⟨FunctionSignature⟩ \text{c'est\ début} ⟨Block⟩ \text{fin}
    \\
    ⟨FunctionSignature⟩;
    \\
    ⟨ProcedureSignature⟩ \text{c'est\ début} ⟨Block⟩ \text{fin}
    \\
    ⟨ProcedureSignature⟩;
    \\
    \text{début} ⟨Block⟩ \text{fin}
    \\
\end{cases}

\\&\textbf{Statements}\notag\\

⟨Statement⟩ &\to \begin{cases}
    ⟨Alternative⟩ \to \begin{split}
    &   ⟨Alternative.If⟩\\
    &   ⟨Alternative.ElseIf⟩^*\\
    &   ⟨Alternative.Else⟩^?\\
    &   \text{finsi}\\
    \end{split}
    \\
    ⟨ProcedureCall⟩ \to ⟨Call⟩;
    \\
    ⟨Assignment⟩ \to ⟨Lvalue⟩ = ⟨Expression⟩;
    \\
    ⟨ForLoop⟩ \to \begin{split}
    \\
    &   \text{pour}\ Identifier
        \ \text{de} ⟨Expression⟩ \text{à} ⟨Expression⟩
        \{\text{pas} ⟨Expression⟩\}^?
        \text{faire} \\
    &   ⟨Block⟩ \\
    &   \text{finfaire}
    \end{split}
    \\
    ⟨RepeatLoop⟩ \to \text{répéter} ⟨Block⟩ \text{jusqu'à} (⟨Expression⟩)
    \\
    ⟨DoWhileLoop⟩ \to \text{faire} ⟨Block⟩ \text{tant\ que} (⟨Expression⟩)
    \\
    ⟨WhileLoop⟩ \to \text{tant\ que} (⟨Expression⟩) \text{faire} ⟨Block⟩ \text{finfaire}
    \\
    ⟨Switch⟩ \to \begin{split}
    &   \text{selon} ⟨Expression⟩ \text{c'est}\\
    &   ⟨Switch.Case⟩^*\\
    &   ⟨Switch.Default⟩^?\\
    &   \text{finselon}\\
    \end{split}
    \\
    ⟨LocalVariable⟩ \to Identifier^{+|,} : ⟨TypeComplete⟩;
    \\
    ⟨Return⟩ \to \text{retourne} ⟨Expression⟩;
    \\
    ⟨BuiltinAssigner⟩ \to \text{assigner}(⟨Lvalue⟩,⟨Expression⟩);
    \\
    ⟨BuiltinEcrire⟩ \to \text{écrire}(⟨Expression⟩, ⟨Expression⟩);
    \\
    ⟨BuiltinEcrireEcran⟩ \to \text{écrireÉcran}(⟨Expression⟩^{*|,});
    \\
    ⟨BuiltinFermer⟩ \to \text{fermer}(⟨Expression⟩);
    \\
    ⟨BuiltinLire⟩ \to \text{lire}(⟨Expression⟩,⟨Lvalue⟩);
    \\
    ⟨BuiltinLireClavier⟩ \to \text{lireClavier}(⟨Lvalue⟩);
    \\
    ⟨BuiltinOuvrirAjout⟩ \to \text{ouvrirAjout}(⟨Expression⟩)
    \\
    ⟨BuiltinOuvrirEcriture⟩ \to \text{ouvrirÉcriture}(⟨Expression⟩);
    \\
    ⟨BuiltinOuvrirLecture⟩ \to \text{ouvrirLecture}(⟨Expression⟩);
    \\
    ⟨Nop⟩ \to ;
\end{cases}
\\
⟨Alternative.If⟩ &\to \text{si} (⟨Expression⟩) \text{alors} ⟨Block⟩
\\
⟨Alternative.ElseIf⟩ &\to \text{sinonsi} (⟨Expression⟩) \text{alors} ⟨Block⟩\
\\
⟨Alternative.Else⟩ &\to \text{sinon} ⟨Block⟩
\\
⟨Switch.Case⟩ &\to \text{quand} ⟨Expression⟩ => ⟨Statement⟩^+\
\\
⟨Switch.Default⟩ &\to \text{quand\ autre} => ⟨Statement⟩^+\

\\&\textbf{Expressions}\notag\\

⟨Expression⟩ &\to \begin{cases}
    ⟨Expression⟩ \text{OU} ⟨Expression_7⟩\\
    ⟨Expression_7⟩
\end{cases}
\\
⟨Expression_7⟩ &\to \begin{cases}
    ⟨Expression_7⟩ \text{ET} ⟨Expression_6⟩\\
    ⟨Expression_6⟩
\end{cases}
\\
⟨Expression_6⟩ &\to \begin{cases}
    ⟨Expression_6⟩ \begin{cases}
        !=\\
        ==\\
    \end{cases} ⟨Expression_5⟩\\
    ⟨Expression_5⟩
\end{cases}
\\
⟨Expression_5⟩ &\to \begin{cases}
    ⟨Expression_5⟩ \begin{cases}
        <\\
        <=\\
        >\\
        >=\\
    \end{cases} ⟨Expression_4⟩\\
    ⟨Expression_4⟩
\end{cases}
\\
⟨Expression_4⟩ &\to \begin{cases}
    ⟨Expression_4⟩ \begin{cases}
        -\\
        +\\
    \end{cases} ⟨Expression_3⟩\\
    ⟨Expression_3⟩
\end{cases}
\\
⟨Expression_3⟩ &\to \begin{cases}
    ⟨Expression_3⟩ \begin{cases}
        *\\
        /\\
        \%\\
    \end{cases} ⟨Expression_2⟩\\
    ⟨Expression_2⟩
\end{cases}
\\
⟨Expression_2⟩ &\to \begin{cases}
    \begin{cases}
        -\\
        +\\
        \text{NON}\\
    \end{cases} ⟨Expression_1⟩\\
    ⟨Expression_1⟩
\end{cases}
\\
⟨Expression_1⟩ &\to \begin{cases}
    ⟨TerminalRvalue⟩\\
    ⟨Lvalue⟩\\
\end{cases}
\\
⟨TerminalRvalue⟩ &\to \begin{cases}
    ⟨Bracketed⟩ \to (⟨Expression⟩)
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
    ⟨BuiltinFdf⟩ \to \text{FdF}(⟨Expression⟩)
    \\
    ⟨TerminalLvalue⟩
\end{cases}
\\
⟨Lvalue⟩ &\to \begin{cases}
    ⟨ArraySubscript⟩\\
    ⟨ComponentAccess⟩\\
    ⟨TerminalLvalue⟩\\
\end{cases}
\\
⟨ArraySubscript⟩ &\to ⟨TerminalRvalue⟩[⟨Expression⟩^{+|,}]
\\
⟨ComponentAccess⟩ &\to ⟨TerminalRvalue⟩.Identifier
\\
⟨TerminalLvalue⟩ &\to \begin{cases}
    ⟨BracketedLvalue⟩ \to (⟨Lvalue⟩)
    \\
    ⟨IdentifierReference⟩ \to Identifier
\end{cases}
\\
⟨Call⟩ &\to Identifier(⟨ParameterActual⟩^{*|,})

\\&\textbf{Types}\notag\\

⟨Type⟩ &\to \begin{cases}
    ⟨TypeComplete⟩
    \\
    ⟨TypeAliasReference⟩ \to Identifier
    \\
    ⟨String⟩ &\to \text{chaîne}
\end{cases}
\\
⟨TypeComplete⟩ &\to \begin{cases}
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
    ⟨StringLengthed⟩ \to \text{chaîne}(⟨Expression⟩)
    \\
    ⟨Structure⟩ \to \text{structure\ début} ⟨LocalVariable⟩^+ \text{fin}
    \\
    ⟨TypeArray⟩ \to \text{tableau} [⟨Expression⟩]^+ \text{de} ⟨TypeComplete⟩
\end{cases}

\\&\textbf{Other}\notag\\

⟨ParameterFormal⟩ &\to \begin{cases}
    \text{entF}\\
    \text{sortF}\\
    \text{entF/sortF}\\
\end{cases} Identifier : ⟨Type⟩
\\
⟨ParameterActual⟩ &\to \begin{cases}
    \text{entE}\\
    \text{sortE}\\
    \text{entE/sortE}\\
\end{cases} ⟨Expression⟩\
\\
⟨FunctionSignature⟩ &\to \text{fonction}\ Identifier(⟨ParameterFormal⟩^{*|,}) \text{délivre} ⟨Type⟩
\\
⟨ProcedureSignature⟩ &\to \text{procédure}\ Identifier(⟨ParameterFormal⟩^{*|,})

\end{align*}
$$
