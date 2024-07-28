$$
\begin{align*}

&\textbf{General} \\

&⟨Algorithm⟩ \to \text{programme}\ Identifier\ \text{c'est} ⟨Declaration⟩^*
\\
&⟨Block⟩ \to ⟨Statement⟩^*

\\\\&\textbf{Declarations} \\

&⟨Declaration⟩ \to \begin{Bmatrix*}[l]
    ⟨DeclarationTypeAlias⟩ \to \text{type}\ Identifier \text{=} ⟨Type⟩ \\
    ⟨DeclarationCompleteTypeAlias⟩ \to \text{type}\ Identifier \text{=} ⟨TypeComplete⟩ \\
    ⟨DeclarationConstant⟩ \to \text{constante} ⟨TypeComplete⟩ Identifier \text{:=} ⟨Expr⟩\text{;} \\
    ⟨MainProgram⟩ \to \text{début} ⟨Block⟩ \text{fin} \\
    ⟨FunctionDeclaration⟩ \to ⟨FunctionSignature⟩ \text{;} \\
    ⟨FunctionDefinition⟩ \to ⟨FunctionSignature⟩ \text{c'est\ début} ⟨Block⟩ \text{fin} \\
    ⟨ProcedureDeclaration⟩ \to ⟨Procedureignature⟩ \text{;} \\
    ⟨ProcedureDefinition⟩ \to ⟨ProcedureSignature⟩ \text{c'est\ début} ⟨Block⟩ \text{fin} \\
    \text{début} ⟨Block⟩ \text{fin} \\
\end{Bmatrix*}

\\&\textbf{Statements} \\

&⟨Statement⟩ \to \begin{Bmatrix*}[l]
    ⟨Alternative⟩ \to \begin{split}
    &   ⟨Alternative.If⟩ \\
    &   ⟨Alternative.ElseIf⟩^* \\
    &   ⟨Alternative.Else⟩^? \\
    &   \text{finsi} \\
    \end{split} \\
    ⟨ProcedureCall⟩ \to ⟨Call⟩\text{;} \\
    ⟨Assignment⟩ \to ⟨Lvalue⟩\text{=}⟨Expr⟩\text{;} \\
    ⟨ForLoop⟩ \to \begin{split}
    &   \begin{Bmatrix*}[l]
            \text{pour}\ Identifier\ \text{de} ⟨Expr⟩ \text{à} ⟨Expr⟩ \begin{pmatrix}\text{pas} ⟨Expr⟩\end{pmatrix}^? \\
            \text{pour}\text{(}Identifier\ \text{de} ⟨Expr⟩ \text{à} ⟨Expr⟩ \begin{pmatrix}\text{pas} ⟨Expr⟩\end{pmatrix}^?\text{)} \\
        \end{Bmatrix*} \\
    &   \text{faire} ⟨Block⟩ \text{finfaire} \\
    \end{split} \\
    ⟨RepeatLoop⟩ \to \text{répéter} ⟨Block⟩ \text{jusqu'à} ⟨Expr⟩ \\
    ⟨DoWhileLoop⟩ \to \text{faire} ⟨Block⟩ \text{tant\ que} ⟨Expr⟩ \\
    ⟨WhileLoop⟩ \to \text{tant\ que} ⟨Expr⟩ \text{faire} ⟨Block⟩ \text{finfaire} \\
    ⟨Switch⟩ \to \begin{split}
    &   \text{selon} ⟨Expr⟩ \text{c'est} \\
    &   ⟨Switch.Case⟩^* \\
    &   ⟨Switch.Default⟩^? \\
    &   \text{finselon} \\
    \end{split} \\
    ⟨LocalVariable⟩ \to ⟨NameTypeBinding⟩\text{:=}⟨Initializer⟩\text{;} \\
    ⟨Return⟩ \to \text{retourne} ⟨Expr⟩\text{;} \\
    ⟨BuiltinAssigner⟩ \to \text{assigner}\text{(}⟨Lvalue⟩\text{,}⟨Expr⟩\text{)}\text{;} \\
    ⟨BuiltinEcrire⟩ \to \text{écrire}\text{(}⟨Expr⟩\text{,}⟨Expr⟩\text{)}\text{;} \\
    ⟨BuiltinEcrireEcran⟩ \to \text{écrireÉcran}\text{(}⟨Expr⟩^{*\#}\text{)}\text{;} \\
    ⟨BuiltinFermer⟩ \to \text{fermer}\text{(}⟨Expr⟩\text{)}\text{;} \\
    ⟨BuiltinLire⟩ \to \text{lire}\text{(}⟨Expr⟩\text{,}⟨Lvalue⟩\text{)}\text{;} \\
    ⟨BuiltinLireClavier⟩ \to \text{lireClavier}\text{(}⟨Lvalue⟩\text{)}\text{;} \\
    ⟨BuiltinOuvrirAjout⟩ \to \text{ouvrirAjout}\text{(}⟨Expr⟩\text{)}\text{;} \\
    ⟨BuiltinOuvrirEcriture⟩ \to \text{ouvrirÉcriture}\text{(}⟨Expr⟩\text{)}\text{;} \\
    ⟨BuiltinOuvrirLecture⟩ \to \text{ouvrirLecture}\text{(}⟨Expr⟩\text{)}\text{;} \\
    ⟨Nop⟩ \to \text{;} \\
\end{Bmatrix*}
\\
&⟨Alternative.If⟩ \to \text{si} ⟨Expr⟩ \text{alors} ⟨Block⟩
\\
&⟨Alternative.ElseIf⟩ \to \text{sinonsi} ⟨Expr⟩ \text{alors} ⟨Block⟩
\\
&⟨Alternative.Else⟩ \to \text{sinon} ⟨Block⟩
\\
&⟨Switch.Case⟩ \to \text{quand} ⟨Expr⟩ \text{=>} ⟨Statement⟩^+
\\
&⟨Switch.Default⟩ \to \text{quand\ autre} \text{=>} ⟨Statement⟩^+
\\
&⟨Initializer⟩ \to \begin{Bmatrix*}[l]
    ⟨Expr⟩ \\
    ⟨Braced⟩ \to \text{\{}
        \begin{pmatrix}
            \begin{pmatrix}
                \begin{Bmatrix*}
                    ⟨Index⟩ \\
                    ⟨Component⟩ \\
                \end{Bmatrix*}
                \text{:=}
            \end{pmatrix}^?
            ⟨Initializer⟩
        \end{pmatrix}^{*\#}
    \text{\}} \\
\end{Bmatrix*}
\\
&⟨Component⟩ \to \text{.}Identifier
\\
&⟨Index⟩ \to \text{[}⟨Expr⟩^{+\#}\text{]}

\\\\&\textbf{Expressions} \\

&⟨Expr⟩ \to ⟨Expr_{Or}⟩
\\
&⟨Expr_{Or}⟩ \to ⟨Expr_{And}⟩\begin{pmatrix}
    \text{OU}
    ⟨Expr_{And}⟩
\end{pmatrix}^*
\\
&⟨Expr_{And}⟩ \to ⟨Expr_{Xor}⟩\begin{pmatrix}
    \text{ET}
    ⟨Expr_{Xor}⟩
\end{pmatrix}^*
\\
&⟨Expr_{Xor}⟩ \to ⟨Expr_{Equality}⟩\begin{pmatrix}
    \text{XOR}
    ⟨Expr_{Equality}⟩
\end{pmatrix}^*
\\
&⟨Expr_{Equality}⟩ \to ⟨Expr_{Comparison}⟩\begin{pmatrix}
\begin{Bmatrix*}[l]
    \text{!=} \\
    \text{==} \\
\end{Bmatrix*}
⟨Expr_{Comparison}⟩\end{pmatrix}^*
\\
&⟨Expr_{Comparison}⟩ \to ⟨Expr_{AddSub}⟩\begin{pmatrix}
\begin{Bmatrix*}[l]
    \text{<} \\
    \text{<=} \\
    \text{>} \\
    \text{>=} \\
\end{Bmatrix*}
⟨Expr_{AddSub}⟩\end{pmatrix}^*
\\
&⟨Expr_{AddSub}⟩ \to ⟨Expr_{MulDivMod}⟩\begin{pmatrix}
\begin{Bmatrix*}[l]
    \text{-} \\
    \text{+} \\
\end{Bmatrix*}
⟨Expr_{MulDivMod}⟩\end{pmatrix}^*
\\
&⟨Expr_{MulDivMod}⟩ \to ⟨Expr_{Unary}⟩\begin{pmatrix}
\begin{Bmatrix*}[l]
    \text{*} \\
    \text{/} \\
    \text{\%} \\
\end{Bmatrix*}
⟨Expr_{Unary}⟩\end{pmatrix}^*
\\
&⟨Expr_{Unary}⟩ \to \begin{Bmatrix*}[l]
    \begin{Bmatrix*}[l]
        \text{-} \\
        \text{+} \\
        \text{NON} \\
    \end{Bmatrix*} ⟨Expr_{Unary}⟩ \\
    ⟨Expr_{Primary}⟩ \\
\end{Bmatrix*}
\\
&⟨Expr_{Primary}⟩ \to \begin{Bmatrix*}[l]
    ⟨Lvalue⟩ \\
    ⟨TerminalRvalue⟩ \\
\end{Bmatrix*}
\\
&⟨Lvalue⟩ \to \begin{Bmatrix*}[l]
    ⟨ArraySubscript⟩ \\
    ⟨ComponentAccess⟩ \\
    ⟨TerminalLvalue⟩ \\
\end{Bmatrix*}
\\
&⟨ArraySubscript⟩ \to ⟨TerminalRvalue⟩⟨Index⟩^+
\\
&⟨ComponentAccess⟩ \to ⟨TerminalRvalue⟩⟨Component⟩^+
\\
&⟨TerminalLvalue⟩ \to \begin{Bmatrix*}[l]
    ⟨BracketedLvalue⟩ \to \text{(}⟨Lvalue⟩\text{)} \\
    ⟨IdentifierReference⟩ \to Identifier \\
\end{Bmatrix*}
\\
&⟨TerminalRvalue⟩ \to \begin{Bmatrix*}[l]
    ⟨Bracketed⟩ \to \text{(}⟨Expr⟩\text{)} \\
    ⟨Literal⟩ \to \begin{Bmatrix*}[l]
        LiteralCharacter \\
        LiteralInteger \\
        LiteralReal \\
        LiteralString \\
        \text{vrai} \\
        \text{faux} \\
    \end{Bmatrix*} \\
    ⟨Call⟩ \\
    ⟨BuiltinFdf⟩ \to \text{FdF}\text{(}⟨Expr⟩\text{)} \\
    ⟨TerminalLvalue⟩ \\
\end{Bmatrix*}
\\
&⟨Call⟩ \to Identifier\text{(}⟨ParameterActual⟩^{*\#}\text{)}

\\\\&\textbf{Types} \\

&⟨Type⟩ \to \begin{Bmatrix*}[l]
    ⟨TypeComplete⟩ \\
    ⟨TypeAliasReference⟩ \to Identifier \\
    ⟨String⟩ \to \text{chaîne} \\
\end{Bmatrix*}
\\
&⟨TypeComplete⟩ \to \begin{Bmatrix*}[l]
    ⟨TypeCompleteAliasReference⟩ \to Identifier \\
    ⟨TypeNumeric⟩ \to \begin{Bmatrix*}[l]
        \text{booléen} \\
        \text{caractère} \\
        \text{entier} \\
        \text{réel} \\
    \end{Bmatrix*} \\
    ⟨File⟩ \to \text{nomFichierLog} \\
    ⟨StringLengthed⟩ \to \text{chaîne}\text{(}⟨Expr⟩\text{)} \\
    ⟨Structure⟩ \to \text{structure\ début} \begin{pmatrix}⟨NameTypeBinding⟩\text{;}\end{pmatrix}^+ \text{fin} \\
    ⟨TypeArray⟩ \to \text{tableau} \begin{pmatrix}\text{[}⟨Expr⟩\text{]}\end{pmatrix}^+ \text{de} ⟨TypeComplete⟩ \\
\end{Bmatrix*}

\\\\&\textbf{Other} \\

&⟨NameTypeBinding⟩ \to Identifier^{+\#} \text{:} ⟨TypeComplete⟩
\\
&⟨ParameterFormal⟩ \to \begin{Bmatrix*}[l]
    \text{entF} \\
    \text{sortF} \\
    \text{entF/sortF} \\
\end{Bmatrix*} Identifier\text{:}⟨Type⟩
\\
&⟨ParameterActual⟩ \to \begin{Bmatrix*}[l]
    \text{entE} \\
    \text{sortE} \\
    \text{entE/sortE} \\
\end{Bmatrix*} ⟨Expr⟩
\\
&⟨FunctionSignature⟩ \to \text{fonction}\ Identifier\text{(}⟨ParameterFormal⟩^{*\#}\text{)} \text{délivre} ⟨Type⟩
\\
&⟨ProcedureSignature⟩ \to \text{procédure}\ Identifier\text{(}⟨ParameterFormal⟩^{*\#}\text{)}

\end{align*}
$$
