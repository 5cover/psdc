# Psdc

IUT de Lannion Pseudocode compiler (transpiler).

## Tools

[ast-gen Python](https://github.com/5cover/ast-gen)

[VSCode Pseudocode extension](https://marketplace.visualstudio.com/items?itemName=NoanPerrot.pseudocode)

## Philosophy

Psdc is a transpiler, designed to automate the painstaking task of rewriting Pseudocode programs to other languages.

Given any Pseudocode program (valid or invalid), Psdc produces an equivalent program in the target language to the best of its ability, providing helpful diagnostics along the way.

<q>Equivalent</q> here is defined as:

- **Equivalence in validity**: a valid Pseudocode program transpiles to a valid program in the target language. An invalid program (with compiler errors) may be invalid in the target language.
- **Equivalence in behavior**: a valid Pseudocode program exhibits the expected behavior in the target language. Since Pseudocode programs can't be executed, the expected behavior of a program is determined by the Standard (yet to be written).
- **Equivalence in semantics**: a Pseudocode program and its transpiled counterpart must be semantically equivalent. This implies:
  - same identifiers (except where target language keywords are used)
  - same declaration order (as possible considering the rules of the target language)
- **Equivalence in representation**: the generated code should be human-readable and easily modifiable, with a clear correspondence between the input pseudocode and the output code.

More information in [The Zen of Pseudocode](<https://github.com/5cover/psdc/wiki/The Zen of Pseudocode>).

## Example

### 1. Input Pseudocode ([sudoku.psc](testPrograms/sudoku.psc))

My pitiful program for S1.01

```psc
/*
Algorithme programme principal Sudoku
*/
programme Sudoku c'est

// Un jeu de Sudoku
constante entier N := 3;
constante entier NB_FICHIERS_GRILLES := 10;
constante entier LONGUEUR_MAX_COMMANDE := 64;
constante entier COTE_GRILLE := N * N;

type t_grilleEntier = tableau[COTE_GRILLE, COTE_GRILLE] de entier;

début
    grilleJeu : t_grilleEntier;
    ligneCurseur, colonneCurseur : entier;
    partieAbandonnée : booléen;
    commandeRéussie : booléen;
    commande : chaîne(LONGUEUR_MAX_COMMANDE);

    partieAbandonnée := faux;
    ligneCurseur := 1;
    colonneCurseur := 1;

    chargerGrille(entE entierAléatoire(entE 1, entE NB_FICHIERS_GRILLES), sortE grilleJeu);

    // Boucle principale du jeu
    faire
        écrireGrille(entE grilleJeu);
        faire
            commande := entréeCommande();
            commandeRéussie := exécuterCommande(entE commande,
                                                entE/sortE grilleJeu,
                                                entE/sortE ligneCurseur,
                                                entE/sortE colonneCurseur,
                                                sortE partieAbandonnée);
        tant que (NON commandeRéussie)
    tant que (NON partieAbandonnée ET NON estGrilleComplète(entE grilleJeu))

    // La partie n'a pas été abandonnée, elle s'est donc terminée par une victoire
    si (NON partieAbandonnée) alors
        écrireEcran("Bravo, vous avez gagné !");
    finsi
fin
```

### 2. Psdc invocation

Let's translate it to C:

<pre>
<code>$ psdc c sudoku.psc -o sudoku.c</code>
<samp>
testPrograms/sudoku.psc:25.24-39: <span style="color: #f14c4c">P0002: error:</span> undefined function or procedure `entierAléatoire`
    25 |     chargerGrille(entE <span style="color: #f14c4c">entierAléatoire</span>(entE 1, entE NB_FICHIERS_GRILLES), sortE grilleJeu);
       |                        <span style="color: #f14c4c">^^^^^^^^^^^^^^^</span>

testPrograms/sudoku.psc:25.5-18: <span style="color: #f14c4c">P0002: error:</span> undefined function or procedure `chargerGrille`
    25 |     <span style="color: #f14c4c">chargerGrille</span>(entE <span style="color: #f14c4c">entierAléatoire</span>(entE 1, entE NB_FICHIERS_GRILLES), sortE grilleJeu);
       |     <span style="color: #f14c4c">^^^^^^^^^^^^^</span>

testPrograms/sudoku.psc:25.24-39: <span style="color: #f14c4c">P0002: error:</span> undefined function or procedure `entierAléatoire`
    25 |     chargerGrille(entE <span style="color: #f14c4c">entierAléatoire</span>(entE 1, entE NB_FICHIERS_GRILLES), sortE grilleJeu);
       |                        <span style="color: #f14c4c">^^^^^^^^^^^^^^^</span>

testPrograms/sudoku.psc:38.43-60: <span style="color: #f14c4c">P0002: error:</span> undefined function or procedure `estGrilleComplète`
    38 |     tant que (NON partieAbandonnée ET NON <span style="color: #f14c4c">estGrilleComplète</span>(entE grilleJeu))
       |                                           <span style="color: #f14c4c">^^^^^^^^^^^^^^^^^</span>

testPrograms/sudoku.psc:29.9-21: <span style="color: #f14c4c">P0002: error:</span> undefined function or procedure `écrireGrille`
    29 |         <span style="color: #f14c4c">écrireGrille</span>(entE grilleJeu);
       |         <span style="color: #f14c4c">^^^^^^^^^^^^</span>

testPrograms/sudoku.psc:31.25-39: <span style="color: #f14c4c">P0002: error:</span> undefined function or procedure `entréeCommande`
    31 |             commande := <span style="color: #f14c4c">entréeCommande</span>();
       |                         <span style="color: #f14c4c">^^^^^^^^^^^^^^</span>

testPrograms/sudoku.psc:32.32-48: <span style="color: #f14c4c">P0002: error:</span> undefined function or procedure `exécuterCommande`
    32 |             commandeRéussie := <span style="color: #f14c4c">exécuterCommande</span>(entE commande,
       |                                <span style="color: #f14c4c">^^^^^^^^^^^^^^^^</span>

Compilation <span style="color: #f14c4c">failed</span> (7 errors, 0 warnings, 0 suggestions).
</samp></pre>

Oops. Looks like we got some errors. Psdc detected that the functions and procedures called aren't defined.

That doesn't prevent it from giving us meaningful output, though.

### 3. C output (sudoku.c)

```c
/** @file
 * @brief Sudoku
 * @author raphael
 * @date 12/09/2024
 */

#include <stdbool.h>
#include <stdio.h>
#include <stdlib.h>

#define N 3
#define NB_FICHIERS_GRILLES 10
#define LONGUEUR_MAX_COMMANDE 64
#define COTE_GRILLE (N * N)

typedef int t_grilleEntier[COTE_GRILLE][COTE_GRILLE];

int main() {
    t_grilleEntier grilleJeu;
    int ligneCurseur, colonneCurseur;
    bool partieAbandonnée;
    bool commandeRéussie;
    char commande[LONGUEUR_MAX_COMMANDE];
    partieAbandonnée = false;
    ligneCurseur = 1;
    colonneCurseur = 1;
    chargerGrille(entierAléatoire(1, NB_FICHIERS_GRILLES), grilleJeu);
    do {
        écrireGrille(grilleJeu);
        do {
            commande = entréeCommande();
            commandeRéussie = exécuterCommande(commande, grilleJeu, &ligneCurseur, &colonneCurseur, &partieAbandonnée);
        } while (!commandeRéussie);
    } while (!partieAbandonnée && !estGrilleComplète(grilleJeu));
    if (!partieAbandonnée) {
        printf("Bravo, vous avez gagné !\n");
    }

    return EXIT_SUCCESS;
}
```

And there you have it. Automated translation between Pseudocode and C.

## Roadmap

### Target languages

- [x] C
- [ ] LLVM
- [ ] C#
- [ ] CimPU
- [ ] Java
- [ ] JavaScript
- [ ] Pascal
- [ ] Perl
- [ ] PHP
- [ ] Python
- [ ] Shell
- [ ] SQL

### Language features

- [x] Formal grammar
- [x] Alternatives
- [x] Loops
  - [x] For
  - [x] While
  - [x] Do..While
  - [x] Repeat..Until
- [x] Procedures
- [x] Functions
- [x] Structures
- [x] `selon`
- [x] Fix syntax error handling
- [x] Lvalues
- [x] Constant folding for type checking and division by zero
- [x] Optional brackets in control structures
- [x] Benchmarks
- [x] Brace initialization (see TD14 ex 1)
- [x] Compiler directives
- [x] Contextual keywords
- [x] `finPour` keyword (equivalent to `fin` but only for loops)
- [x] Escape sequences in string and character literals
- [x] Case-insensitive boolean operators
- [ ] More static analysis
- [ ] Alternative array syntax `tableau[INDICE_DEPART..INDICE_FIN] de type;`
- [ ] Numeroted control stuctures (`si1`, `si2`, `si3`)
- [ ] File handling (low priority)
- [ ] Preprocessor
  - [x] Static assertions
  - [x] Expression/Type probing
  - [ ] Modularity (`#include`)
  - [ ] Conditional compilation
- [ ] Configuration
- [x] [GNU](https://www.gnu.org/prep/standards/standards.html#Errors)-compliant message formatting
- [ ] Translations : resx, fr
- [x] CLI (use nuget package)
  - [ ] custom header
  - [ ] Formatting customization
  - [ ] documentation date: now, file?
- [ ] **Language standard**
- [ ] Tests
  - [ ] Errors
  - [ ] Valid code
- [ ] Documentation
  - [x] CLI
  - [ ] Language tutorials
- [ ] Initial release
- [ ] Sample "real" program
- [ ] Self-hosting (rewrite in Pseudocode)
- [ ] VSCode tooling
  - [ ] Debugger
  - [ ] Language server
  - [x] Better syntax highlighter
- [ ] access to argc and argv
- [ ] transpile comments

### C output configuration

- [ ] Non-null-terminated-string-proof format strings: width specifier for lengthed strings (usually useless since null-terminated, but could be useful if non null-terminated strings are used)
- [ ] Type mappings
  - [ ] `réel` &rarr; `float`, `double`, `long double`?
  - [ ] `entier` &rarr; `short`, `int`, `long`?
  - [ ] `caractère` &rarr; `char`, `tchar_t`, `wchar_t`?
- [ ] Parameter names in prototypes?
- [ ] Doxygen documentation skeleton?
- [ ] `i++` or `++i`
- [ ] Use count-based string functions: strncpy, strncmp...
- [ ] use `puts` instead of `printf` where possible
- [ ] anonymous block in switch cases: always, when multiple statements, when vardecl
- [ ] anonymous block in switch default: always, when multiple statements, when vardecl
- [ ] Doxygen keyword char : `\` or `@`
