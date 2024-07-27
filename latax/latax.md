$$
\begin{align*}

&⟨syntax⟩ \to ⟨\_nl⟩ mdMath ⟨\_nl⟩ alignL ⟨\_nl⟩ \begin{pmatrix} ⟨\_nl⟩ mdMath ⟨\_nl⟩
    ⟨topLevelElement⟩
    ⟨\_nl⟩
\end{pmatrix}^* alignR ⟨\_nl⟩
\\
&⟨topLevelElement⟩ \to amp ⟨\_⟩ \begin{Bmatrix*}[l]
    header \\
    ⟨rule⟩ \\
\end{Bmatrix*}
\\
&⟨rule⟩ \to ruleName ⟨\_⟩ to \begin{pmatrix}
    ⟨\_⟩
    ⟨item⟩
\end{pmatrix}^+
\\
&⟨item⟩ \to \begin{Bmatrix*}[l]
    ⟨cases⟩ \\
    ⟨group⟩ \\
    ⟨split⟩ \\
    ruleName \\
    ⟨terminal⟩ \\
\end{Bmatrix*} \begin{pmatrix}
    ⟨\_⟩
    ⟨quantifier⟩
\end{pmatrix}^?
\\
&⟨cases⟩ \to casesL ⟨\_⟩ \begin{pmatrix}
    ⟨case⟩
    nl
    ⟨\_⟩
\end{pmatrix}^+ casesR
\\
&⟨case⟩ \to \begin{Bmatrix*}[l]
    ⟨rule⟩ ⟨\_⟩ \\
    \begin{pmatrix}⟨item⟩ ⟨\_⟩\end{pmatrix}^+ \\
\end{Bmatrix*}
\\
&⟨group⟩ \to groupL ⟨\_⟩ \begin{pmatrix}
    ⟨item⟩
    ⟨\_⟩
\end{pmatrix}^* groupR
\\
&⟨split⟩ \to splitL ⟨\_⟩ \begin{pmatrix}
    amp ⟨\_⟩
    \begin{pmatrix}
        ⟨item⟩
        ⟨\_⟩
    \end{pmatrix}^+
    nl
    ⟨\_⟩
\end{pmatrix}^+ splitR
\\
&⟨terminal⟩ \to \begin{Bmatrix*}[l]
    text \\
    tokenName \\
\end{Bmatrix*}
\\
&⟨quantifier⟩ \to caret ⟨\_⟩ ⟨quantifierSup⟩
\\
&⟨quantifierSup⟩ \to \begin{Bmatrix*}[l]
    zeroOrOne \\
    zeroOrMore \\
    oneOrMore \\
    braceL ⟨\_⟩ zeroOrMore ⟨\_⟩ csl ⟨\_⟩ braceR \\
    braceL ⟨\_⟩ oneOrMore ⟨\_⟩ csl ⟨\_⟩ braceR \\
\end{Bmatrix*}
\\
&⟨\_⟩ \to ws^*
\\
&⟨\_nl⟩ \to \begin{Bmatrix*}[l]
    nl \\
    ws \\
\end{Bmatrix*}^*

\end{align*}
$$
