# Lannion IUT Pseudocode grammar (LaTaX)

$$
\begin{align*}

&⟨Algorithm⟩ \to ⟨Program⟩^?
\\
&⟨Program⟩ \to \text{programme}\ Identifier\ \text{c'est} ⟨Declaration⟩^*
\\
&⟨Block⟩ \to ⟨Statement⟩^*

\\\\&\textbf{Declarations} \\

&⟨Declaration⟩ \to \begin{Bmatrix*}[l]
    ⟨CompilerDirective⟩ \\
    ⟨Constant⟩ \to \text{constante} ⟨Type⟩ Identifier \text{:=} ⟨Expr⟩\text{;} \\
    ⟨Function⟩ \to ⟨FunctionSignature⟩ \text{;} \\
    ⟨FunctionDefinition⟩ \to ⟨FunctionSignature⟩ \text{c'est}^?\ \text{début} ⟨Block⟩ \text{fin} \\
    ⟨MainProgram⟩ \to \text{début} ⟨Block⟩ \text{fin} \\
    ⟨Nop⟩ \\
    ⟨Procedure⟩ \to ⟨Procedureignature⟩ \text{;} \\
    ⟨ProcedureDefinition⟩ \to ⟨ProcedureSignature⟩ \text{c'est}^?\ \text{début} ⟨Block⟩ \text{fin} \\
    ⟨TypeAlias⟩ \to \text{type}\ Identifier \text{=} ⟨Type⟩ \text{;} \\
\end{Bmatrix*}

\\&\textbf{Statements} \\

&⟨Statement⟩ \to \begin{Bmatrix*}[l]
    ⟨Alternative⟩ \to \begin{split}
    &   ⟨Alternative.If⟩ \\
    &   ⟨Alternative.ElseIf⟩^* \\
    &   ⟨Alternative.Else⟩^? \\
    &   \text{finsi} \\
    \end{split} \\
    ⟨Assignment⟩ \to ⟨Lvalue⟩\text{=}⟨Expr⟩\text{;} \\
    ⟨BuiltinAssigner⟩ \to \text{assigner}\text{(}⟨Lvalue⟩\text{,}⟨Expr⟩\text{)}\text{;} \\
    ⟨BuiltinEcrire⟩ \to \text{écrire}\text{(}⟨Expr⟩\text{,}⟨Expr⟩\text{)}\text{;} \\
    ⟨BuiltinEcrireEcran⟩ \to \text{écrireÉcran}\text{(}⟨Expr⟩^{*\#}\text{)}\text{;} \\
    ⟨BuiltinFermer⟩ \to \text{fermer}\text{(}⟨Expr⟩\text{)}\text{;} \\
    ⟨BuiltinLire⟩ \to \text{lire}\text{(}⟨Expr⟩\text{,}⟨Lvalue⟩\text{)}\text{;} \\
    ⟨BuiltinLireClavier⟩ \to \text{lireClavier}\text{(}⟨Lvalue⟩\text{)}\text{;} \\
    ⟨BuiltinOuvrirAjout⟩ \to \text{ouvrirAjout}\text{(}⟨Expr⟩\text{)}\text{;} \\
    ⟨BuiltinOuvrirEcriture⟩ \to \text{ouvrirÉcriture}\text{(}⟨Expr⟩\text{)}\text{;} \\
    ⟨BuiltinOuvrirLecture⟩ \to \text{ouvrirLecture}\text{(}⟨Expr⟩\text{)}\text{;} \\
    ⟨CompilerDirective⟩\\
    ⟨DoWhileLoop⟩ \to \text{faire} ⟨Block⟩ \text{tant\ que} ⟨Expr⟩ \\
    ⟨ForLoop⟩ \to \begin{split}
    &   \begin{Bmatrix*}[l]
            \text{pour}\ Identifier\ \text{de} ⟨Expr⟩ \text{à} ⟨Expr⟩ \begin{pmatrix}\text{pas} ⟨Expr⟩\end{pmatrix}^? \\
            \text{pour}\text{(}Identifier\ \text{de} ⟨Expr⟩ \text{à} ⟨Expr⟩ \begin{pmatrix}\text{pas} ⟨Expr⟩\end{pmatrix}^?\text{)} \\
        \end{Bmatrix*} \\
    &   \text{faire} ⟨Block⟩ \text{finfaire} \\
    \end{split} \\
    ⟨LocalVariable⟩ \to ⟨VariableDeclaration⟩\begin{pmatrix}\text{:=}⟨Initializer⟩\end{pmatrix}^?\text{;} \\
    ⟨Nop⟩ \\
    ⟨RepeatLoop⟩ \to \text{répéter} ⟨Block⟩ \text{jusqu'à} ⟨Expr⟩ \\
    ⟨Return⟩ \to \text{retourne} ⟨Expr⟩^?\text{;} \\
    ⟨Switch⟩ \to \begin{split}
    &   \text{selon} ⟨Expr⟩ \text{c'est} \\
    &   \begin{Bmatrix*}[l]
            \text{quand} ⟨Expr⟩ \text{=>} ⟨Statement⟩^* \\
            \text{quand\ autre} \text{=>} ⟨Statement⟩^* \\
        \end{Bmatrix*}^+ \\
    &   \text{finselon} \\
    \end{split} \\
    ⟨WhileLoop⟩ \to \text{tant\ que} ⟨Expr⟩ \text{faire} ⟨Block⟩ \text{finfaire} \\
    ⟨Expr⟩ \text{;} \\
\end{Bmatrix*}
\\
&⟨Alternative.If⟩ \to \text{si} ⟨Expr⟩ \text{alors} ⟨Block⟩
\\
&⟨Alternative.ElseIf⟩ \to \text{sinonsi} ⟨Expr⟩ \text{alors} ⟨Block⟩
\\
&⟨Alternative.Else⟩ \to \text{sinon} ⟨Block⟩

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
    ⟨BuiltinFdf⟩ \to \text{FdF}\text{(}⟨Expr⟩\text{)} \\
    ⟨Call⟩ \to Identifier\text{(}⟨ParameterActual⟩^{*\#}\text{)} \\
    ⟨Literal⟩ \to \begin{Bmatrix*}[l]
        LiteralCharacter \\
        LiteralInteger \\
        LiteralReal \\
        LiteralString \\
        \text{vrai} \\
        \text{faux} \\
    \end{Bmatrix*} \\
    ⟨TerminalLvalue⟩ \\
\end{Bmatrix*}

\\\\&\textbf{Types} \\

&⟨Type⟩ \to \begin{Bmatrix*}[l]
    ⟨AliasReference⟩ \to Identifier \\
    ⟨Array⟩ \to \text{tableau} \begin{pmatrix}\text{[}⟨Expr⟩\text{]}\end{pmatrix}^+ \text{de} ⟨Type⟩ \\
    ⟨Boolean⟩ \to \text{booléen} \\
    ⟨Character⟩ \to \text{caractère} \\
    ⟨File⟩ \to \text{nomFichierLog} \\
    ⟨Integer⟩ \to \text{entier} \\
    ⟨Real⟩ \to \text{réel} \\
    ⟨String⟩ \to \text{chaîne} \\
    ⟨StringLengthed⟩ \to \text{chaîne}\text{(}⟨Expr⟩\text{)} \\
    ⟨Structure⟩ \to \text{structure\ début} ⟨Component⟩^+ \text{fin} \\
\end{Bmatrix*}
\\
&⟨Component⟩ \to \begin{Bmatrix*}[l]
    ⟨CompilerDirective⟩ \\
    ⟨VariableDeclaration⟩\text{;} \\
\end{Bmatrix*}

\\\\&\textbf{Other} \\

&⟨CompilerDirective⟩ \to \begin{Bmatrix*}[l]
    \text{\#}\text{assert}⟨Expr⟩⟨Expr⟩^?\\
    \text{\#}\text{eval}\ \text{expr}⟨Expr⟩\\
    \text{\#}\text{eval}\ \text{type}⟨Type⟩\\
\end{Bmatrix*}
\\
&⟨ParameterFormal⟩ \to \begin{Bmatrix*}[l]
    \text{entF} \\
    \text{sortF} \\
    \text{entF/sortF} \\
\end{Bmatrix*}Identifier\text{:}⟨Type⟩
\\
&⟨ParameterActual⟩ \to \begin{Bmatrix*}[l]
    \text{entE} \\
    \text{sortE} \\
    \text{entE/sortE} \\
\end{Bmatrix*}⟨Expr⟩
\\
&⟨VariableDeclaration⟩ \to Identifier^{+\#}\text{:}⟨Type⟩
\\
&⟨FunctionSignature⟩ \to \text{fonction}\ Identifier\text{(}⟨ParameterFormal⟩^{*\#}\text{)} \text{délivre} ⟨Type⟩
\\
&⟨ProcedureSignature⟩ \to \text{procédure}\ Identifier\text{(}⟨ParameterFormal⟩^{*\#}\text{)}
\\
&⟨Nop⟩ \to \text{;}
\\
&⟨Initializer⟩ \to \begin{Bmatrix*}[l]
    ⟨Expr⟩\\
    ⟨Braced⟩ \to \text{\{}
        \begin{Bmatrix*}[l]
            \begin{pmatrix}
                ⟨Designator⟩^+
                \text{:=}
            \end{pmatrix}^?
            ⟨Initializer⟩ \\
            ⟨CompilerDirective⟩ \\
        \end{Bmatrix*}^{*\#}
    \text{\}}\\
\end{Bmatrix*}
\\
&⟨Designator⟩ \to \begin{Bmatrix*}[l]
    ⟨Component⟩ \\
    ⟨Index⟩ \\
\end{Bmatrix*}
\\
&⟨Component⟩ \to \text{.}Identifier
\\
&⟨Index⟩ \to \text{[}⟨Expr⟩^{+\#}\text{]}

\end{align*}
$$
