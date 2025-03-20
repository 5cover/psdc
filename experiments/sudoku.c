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
