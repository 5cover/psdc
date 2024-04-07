# Formal grammar

Détermine l'aspect syntaxique (et non pas sémantique) d'un programme.

**Terminal** : symboles élémentaires du langage

**Non-terminal** : groupes de symboles terminaux déterminés par les règles de production.

3 types of rules :

- Case rules : only a list of choice
    - Pixeled C++ : variant
    - My C++ : union?
    - C# : inherit empty base class
- Terminals : rules that don't reference any other rules (leaves)

## Règles de production

symbole|légende
-|-
⟨PascalCalse⟩|règle de production
camelCase|token
*camelCase*|match
\*|zéro ou plus
\+|un ou plus
\?|facultatif
\|x|séparé par x

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

⟨Algorithme⟩ &\to ⟨Déclaration⟩^*
\\
⟨Bloc⟩ &\to ⟨Instruction⟩^*

\\&\textbf{Déclarations}\notag\\

⟨Déclaration⟩ &\to \begin{cases}
    ⟨DéclarationAlias⟩\\
    ⟨DéclarationConstante⟩\\
    ⟨ProgrammePrincipal⟩\\
    ⟨SignatureFonction⟩ \text{c'est\ début} ⟨Bloc⟩ \text{fin}\\
    ⟨SignatureFonction⟩;\\
    ⟨SignatureProcédure⟩ \text{c'est\ début} ⟨Bloc⟩ \text{fin}\\
    ⟨SignatureProcédure⟩;\\
    \text{début} ⟨Bloc⟩ \text{fin}\\
\end{cases}
\\
⟨DéclarationAlias⟩ &\to \text{type}\ identifiant =\begin{cases}
    ⟨TypeComplet⟩\\
    ⟨DéfinitionStructure⟩\\
\end{cases};
\\
⟨DéclarationConstante⟩ &\to \text{constante} ⟨TypeComplet⟩ identifiant \text{:=} ⟨Expression⟩;
\\
⟨DéfinitionStructure⟩ &\to \text{structure\ début} ⟨DéclarationVariable⟩^+ \text{fin}
\\
⟨ProgrammePrincpal⟩ &\to \text{programme}\ identifiant\ \text{c'est\ début} ⟨Bloc⟩ \text{fin}
\\
⟨SignatureFonction⟩ &\to \text{fonction}\ identifiant(⟨ParamètreFormel⟩^{*|,}) \text{délivre} ⟨Type⟩
\\
⟨SignatureProcédure⟩ &\to \text{procédure}\ identifiant(⟨ParamètreFormel⟩^{*|,})
\\
⟨ParamètreFormel⟩ &\to \begin{cases}
    \text{entF}\\
    \text{sortF}\\
    \text{entF/sortF}\\
\end{cases} identifiant : ⟨Type⟩

\\&\textbf{Instructions}\notag\\

⟨Instruction⟩ &\to \begin{cases}
    ⟨Alternative⟩\\
    ⟨Assignation⟩\\
    ⟨BouclePour⟩\\
    ⟨BoucleRépéter⟩\\
    ⟨BoucleFaireTantQue⟩\\
    ⟨BoucleTantQue⟩\\
    ⟨Selon⟩\\
    ⟨DéclarationVariable⟩\\
    \text{retourne} ⟨Expression⟩;\\
    \text{assigner}(⟨Expression⟩, ⟨Expression⟩);\\
    \text{écrire}(⟨Expression⟩, ⟨Expression⟩);\\
    \text{écrireÉcran}(⟨Expression⟩^{+|,});\\
    \text{fermer}(⟨Expression⟩);\\
    \text{lire}(⟨Expression⟩, ⟨Expression⟩);\\
    \text{lireClavier}(⟨Expression⟩);\\
    \text{ouvrirAjout}(⟨Expression⟩);\\
    \text{ouvrirÉcriture}(⟨Expression⟩);\\
    \text{ouvrirLecture}(⟨Expression⟩);\\
\end{cases}
\\
⟨Alternative⟩ &\to \begin{split}
&   \text{si} ⟨Expression⟩ \text{alors} ⟨Bloc⟩\\
&   \{\text{sinonsi} ⟨Expression⟩ \text{alors} ⟨Bloc⟩\}^*\\
&   \{\text{sinon} ⟨Bloc⟩\}^?\\
&   \text{finsi}\\
\end{split}
\\
⟨Assignation⟩ &\to identifiant = ⟨Expression⟩;
\\
⟨BouclePour⟩ &\to \begin{split}
&    \text{pour}\ identifiant
    \ \text{de} ⟨Expression⟩ \text{à} ⟨Expression⟩
    \{\text{pas} ⟨Expression⟩\}^?
    \text{faire} \\
&   ⟨Bloc⟩ \\
&   \text{finfaire}
\end{split}
\\
⟨BoucleRépéter⟩ &\to \text{répéter} ⟨Bloc⟩ \text{jusqu'à} (⟨Expression⟩)
\\
⟨BoucleFaireTantQue⟩ &\to \text{faire} ⟨Bloc⟩ \text{tant\ que} (⟨Expression⟩)
\\
⟨BoucleTantQue⟩ &\to \text{tant\ que} ⟨Expression⟩ \text{faire} ⟨Bloc⟩ \text{finfaire}
\\
⟨DéclarationVariable⟩ &\to identifiant^{+|,} : ⟨TypeComplet⟩;
\\
⟨Selon⟩ &\to \begin{split}
&   \text{selon} ⟨Expression⟩ \text{c'est}\\
&   \{\text{quand} ⟨Expression⟩ => ⟨Instruction⟩^+\}^*\\
&   \{\text{quand\ autre} => ⟨Instruction⟩^+\}^?\\
&   \text{finselon}\\
\end{split}

\\&\textbf{Expressions}\notag\\

⟨Appel⟩ &\to identifiant(\{\begin{cases}
    \text{entE}\\
    \text{sortE}\\
    \text{entE/sortE}\\
\end{cases} ⟨Expression⟩\}^{*|,})

\\&\textbf{Opérations}\notag\\

⟨Expression_1⟩ &\to \begin{cases}
    (⟨Expression⟩)\\
    ⟨Expression⟩[⟨Expression⟩^{+|,}]\\
    ⟨Appel⟩\\
    ⟨Littéral⟩\\
    identifiant\\
    ⟨Expression⟩.identifiant\\
    \text{FdF}(⟨Expression⟩)\\
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
⟨Expression_3⟩ &\to \begin{cases}
    ⟨Expression_3⟩ \begin{cases}
        *\\
        /\\
        \%\\
    \end{cases} ⟨Expression_2⟩\\
    ⟨Expression_2⟩
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
⟨Expression_6⟩ &\to \begin{cases}
    ⟨Expression_6⟩ \begin{cases}
        !=\\
        ==\\
    \end{cases} ⟨Expression_5⟩\\
    ⟨Expression_5⟩
\end{cases}
\\
⟨Expression_7⟩ &\to \begin{cases}
    ⟨Expression_7⟩ \text{ET} ⟨Expression_6⟩\\
    ⟨Expression_6⟩
\end{cases}
\\
⟨Expression⟩ &\to \begin{cases}
    ⟨Expression⟩ \text{OU} ⟨Expression_7⟩\\
    ⟨Expression_7⟩
\end{cases}

\\&\textbf{Types}\notag\\

⟨Type⟩ &\to \begin{cases}
    \text{chaîne}\\
    ⟨TypeComplet⟩\\
\end{cases}
\\
⟨TypeComplet⟩ &\to \begin{cases}
    \text{chaîne}(⟨Expression⟩)\\
    ⟨TypePrimitif⟩\\
    ⟨TypeTableau⟩\\
    identifiant\\
\end{cases}
\\
⟨TypeTableau⟩ &\to \text{tableau} [⟨Expression⟩]^+ \text{de} ⟨TypeComplet⟩

\\&\textbf{Terminaux}\notag\\

⟨Littéral⟩ &\to \begin{cases}
    \text{littéralChaîne}\\
    \text{littéralCaractère}\\
    \text{littéralEntier}\\
    \text{littéralRéel}\\
    \text{vrai}\\
    \text{faux}\\
\end{cases}
\\
⟨TypePrimitif⟩ &\to \begin{cases}
    \text{booléen}\\
    \text{caractère}\\
    \text{entier}\\
    \text{nomFichierLog}\\
    \text{réel}\\
\end{cases}

\end{align*}
$$
