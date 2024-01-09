# Parser

**Syntax node** : singular, stand-alone unit of meaning in a program that can be individually translated.

Objectif : faire une version ultra simplifiée du projet afin de mieux comprendre de quoi il retourne, et de tester des solutions.

Implémentation : définir une représentation en mémoire du programme sous la forme d'un arbre.

Cette arbre peut être utilisé pour produire n'importe quelle représentation (C, français, Python...)

## Pourquoi utiliser un arbre?

Cela permet de définir une notion de profondeur qui revient à la notion de portée.

La racine représenterait l'élément programme, les élements de niveau 1 les en-têtes, alias, constantes, les fonctions, le procédures et le programme principal, le niveau 2 le corps des fonctions/procédures/programme principal...

## Construction de l'arbre

Comment construire l'arbre ?

Il faut définir des primitifs sur lesquels seront basés tous les élements du langage.

Token :

- une position (ligne, colonne)

Deux types d'élements : non-terminaux (construits à partir d'autres éléments) et les terminaux (un seul token).
Les non-terminaux corresondent aux fonctionnalités du langage et sont construits avec des terminaux

L'arbre contient uniquement des non-terminaux mais ceux-ci donnent l'accès à leurs élements (terminaux ou non-terminaux) internes.

Exemple de terminaux :

- Literal (littéral)
    - LiteralString
    - LiteralInteger
    - LiteralFloat
- Identifier (identifiant)

Exemple de non-terminaux :

- Expression

Processus de lecture :

Lire token par token, mettre à jour une liste des terminaux candidats, quand il ne reste plus qu'un candidat, l'ajouter à la liste des terminaux lus.
Mettre à jour la liste des non-terminaux candidats à partir de la liste des terminaux lus.

Problème : comment construire un arbre? là on aurait une liste de noeuds.

Réponse : l'arbre n'est pas "libre". Les éléments enfants

Pour la lecture des terminaux et composés :

Si il n'y a plus aucun candidat, c'est une erreur de syntaxe.

Si le non-terminal obtenu n'est pas valide pour cette emplacement dans l'arbre, c'est une erreur de placement des élements. Exemple : une déclaration de variable en dehors d'une fonction.

## Vérification des erreurs

Quand vérifier la syntaxe? Lors de la construction de l'arbre ou en parcourant l'arbre après? Ou les deux?

## new way

Objective : séparer le peeking from le parsing
Savoir qu'on est bon avant de commencer à consommer les tokens.

/*
Token { Type = KeywordProgram, Value = , StartIndex = 0, Length = 9 }
Token { Type = Identifier, Value = HelloWorld, StartIndex = 10, Length = 10 }
Token { Type = KeywordIs, Value = , StartIndex = 21, Length = 5 }
Token { Type = KeywordBegin, Value = , StartIndex = 27, Length = 5 }
Token { Type = KeywordEcrireEcran, Value = , StartIndex = 37, Length = 11 }
Token { Type = OpenBracket, Value = , StartIndex = 48, Length = 1 }
Token { Type = LiteralString, Value = Hello world!, StartIndex = 49, Length = 14 }
Token { Type = CloseBracket, Value = , StartIndex = 63, Length = 1 }
Token { Type = DelimiterTerminator, Value = , StartIndex = 64, Length = 1 }
Token { Type = KeywordEnd, Value = , StartIndex = 66, Length = 3 }
*/

/*
NodeAlgorithm { Name = "HelloWorld" }
    NodeMainProgram
        NodePrintStatement
            NodeLiteralString { Value = "Hello world!" }

*/

/*
// HelloWorld
int main() { printf("Hello world!"); return 0; }
*/

## Expressions

Comment parser des expressions?

Une expression

(a + 5) / 4 * (9 % 'c')

Récursif

    /
      +
        a
        5
      *
        4
        %
          9
          'c'

    BinaryOperation /
      BinaryOperation +
        Identifier a
        LiteralInteger 5
      BinaryOperation *
        LiteralInteger 4
        BinaryOperation %
          LiteralInteger 9
          LiterCharacter c

## Error handling

Erreurs de parsing

Quand le parser rencontre un token innatendu : retourne un ParseResult d'échec avec pour SourceTokens les tokens erronés.

Une fonction de non-terminal n'échoue que quand elle recontre un token innatendu, et non pas quand une autre fonction qu'elle apelle échoue.

Une fonction A parsant * ou + fois B doit continuer même si B échoue, sauf dans le cas où la fin du fichier a été atteint (SourceToken est vide), afin d'éviter les boucles infinies.

Quand le parse réussit : retourne un ParseResult de réussite avec l'instance de noeud et pour SourceTokens les tokens lus.

## new idea from yt comment

quick thoughts (on the parsing):

1. parse the whole text through linear passes first (not recursively)
2. use indexes  to sections in the string (no need for pointer magic)
3. collect the keywords and variables (then can easily check if identifier has already been declared or not) in a vector of indexes (eg: type can be {index int, length int} )
4. after collecting all identifiers and expressions, then simple rules to check if syntax rules obeyed
(eg, math expression has lhs n rhs, expression precedence, identifiers preceeded by let, type system obeyed, etc)

i think biggest benefits:

1. easy to reason algorithmically
2. no need for allocator magic (which looks really cool btw)
3. parsing becomes a linguistics problem not a data structure problem
4. parse through function calls that check if expected expression is found at expected location

because I'm seeing your project already has so many different types (hence needing allocator magic when they recurse),
when there's no need to split from the file/string (which already has all that information encoded)
