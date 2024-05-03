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
    ⟨DeclarationConstant⟩ \to \text{constante} ⟨TypeComplete⟩ Identifier \text{:=} ⟨Expr⟩;
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
    ⟨Assignment⟩ \to ⟨Lvalue⟩ = ⟨Expr⟩;
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
    ⟨LocalVariable⟩ \to Identifier^{+|,} : ⟨TypeComplete⟩;
    \\
    ⟨Return⟩ \to \text{retourne} ⟨Expr⟩;
    \\
    ⟨BuiltinAssigner⟩ \to \text{assigner}(⟨Lvalue⟩,⟨Expr⟩);
    \\
    ⟨BuiltinEcrire⟩ \to \text{écrire}(⟨Expr⟩, ⟨Expr⟩);
    \\
    ⟨BuiltinEcrireEcran⟩ \to \text{écrireÉcran}(⟨Expr⟩^{*|,});
    \\
    ⟨BuiltinFermer⟩ \to \text{fermer}(⟨Expr⟩);
    \\
    ⟨BuiltinLire⟩ \to \text{lire}(⟨Expr⟩,⟨Lvalue⟩);
    \\
    ⟨BuiltinLireClavier⟩ \to \text{lireClavier}(⟨Lvalue⟩);
    \\
    ⟨BuiltinOuvrirAjout⟩ \to \text{ouvrirAjout}(⟨Expr⟩)
    \\
    ⟨BuiltinOuvrirEcriture⟩ \to \text{ouvrirÉcriture}(⟨Expr⟩);
    \\
    ⟨BuiltinOuvrirLecture⟩ \to \text{ouvrirLecture}(⟨Expr⟩);
    \\
    ⟨Nop⟩ \to ;
\end{cases}
\\
⟨Alternative.If⟩ &\to \text{si} ⟨Expr⟩ \text{alors} ⟨Block⟩
\\
⟨Alternative.ElseIf⟩ &\to \text{sinonsi} ⟨Expr⟩ \text{alors} ⟨Block⟩\
\\
⟨Alternative.Else⟩ &\to \text{sinon} ⟨Block⟩
\\
⟨Switch.Case⟩ &\to \text{quand} ⟨Expr⟩ => ⟨Statement⟩^+\
\\
⟨Switch.Default⟩ &\to \text{quand\ autre} => ⟨Statement⟩^+\

\\&\textbf{Exprs}\notag\\

⟨Expr⟩ &\to ⟨Expr_{Or}⟩
\\
⟨Expr_{Or}⟩ &\to ⟨Expr_{And}⟩\{
\text{OU}
⟨Expr_{And}⟩\}^*
\\
⟨Expr_{And}⟩ &\to ⟨Expr_{Xor}⟩\{
\text{ET}
⟨Expr_{Xor}⟩\}^*
\\
⟨Expr_{Xor}⟩ &\to ⟨Expr_{Equality}⟩\{
\text{XOR}
⟨Expr_{Equality}⟩\}^*
\\
⟨Expr_{Equality}⟩ &\to ⟨Expr_{Comparison}⟩\{
\begin{cases}
    !=\\
    ==\\
\end{cases}
⟨Expr_{Comparison}⟩\}^*
\\
⟨Expr_{Comparison}⟩ &\to ⟨Expr_{AddSub}⟩\{
\begin{cases}
    <\\
    <=\\
    >\\
    >=\\
\end{cases}
⟨Expr_{AddSub}⟩\}^*
\\
⟨Expr_{AddSub}⟩ &\to ⟨Expr_{MulDivMod}⟩\{
\begin{cases}
    -\\
    +\\
\end{cases}
⟨Expr_{MulDivMod}⟩\}^*
\\
⟨Expr_{MulDivMod}⟩ &\to ⟨Expr_{Unary}⟩\{
\begin{cases}
    *\\
    /\\
    \%\\
\end{cases}
⟨Expr_{Unary}⟩\}^*
\\
⟨Expr_{Unary}⟩ &\to \begin{cases}
    \begin{cases}
        -\\
        +\\
        \text{NON}\\
    \end{cases} ⟨Expr_{Unary}⟩\\
    ⟨Expr_{Primary}⟩\\
\end{cases}
\\
⟨Expr_{Primary}⟩ &\to \begin{cases}
    ⟨Lvalue⟩\\
    ⟨TerminalRvalue⟩\\
\end{cases}
\\
⟨Lvalue⟩ &\to \begin{cases}
    ⟨ArraySubscript⟩\\
    ⟨ComponentAccess⟩\\
    ⟨TerminalLvalue⟩\\
\end{cases}
\\
⟨ArraySubscript⟩ &\to ⟨TerminalRvalue⟩\{[⟨Expr⟩^{+|,}]\}^+
\\
⟨ComponentAccess⟩ &\to ⟨TerminalRvalue⟩\{.⟨Identifier⟩\}^+
\\
⟨TerminalLvalue⟩ &\to \begin{cases}
    ⟨BracketedLvalue⟩ \to (⟨Lvalue⟩)
    \\
    ⟨IdentifierReference⟩ \to Identifier
\end{cases}
\\
⟨TerminalRvalue⟩ &\to \begin{cases}
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
    ⟨StringLengthed⟩ \to \text{chaîne}(⟨Expr⟩)
    \\
    ⟨Structure⟩ \to \text{structure\ début} ⟨LocalVariable⟩^+ \text{fin}
    \\
    ⟨TypeArray⟩ \to \text{tableau} [⟨Expr⟩]^+ \text{de} ⟨TypeComplete⟩
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
\end{cases} ⟨Expr⟩\
\\
⟨FunctionSignature⟩ &\to \text{fonction}\ Identifier(⟨ParameterFormal⟩^{*|,}) \text{délivre} ⟨Type⟩
\\
⟨ProcedureSignature⟩ &\to \text{procédure}\ Identifier(⟨ParameterFormal⟩^{*|,})

\end{align*}
$$
