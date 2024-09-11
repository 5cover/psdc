/**
 * @file
 * @brief Ex. 6 TD4 Dév.
 * @author rbardini
 * @date 5/10/2023
 *
 * détermine le pourcentage d'étudiants ayant plus de la moyenne à l'issue du DS d'algo
 */
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

char const FIN[] = "*";
int const NOTE_MIN = 0, NOTE_MAX = 20;

int main()
{
    int nbEtudiants = 0;
    int nbEtudiantsAvoirMoyenne = 0;
    float note = 0;
    char nom[20];

    do {
        printf("Nom de l'étudiant : ");
        scanf("%s", nom);
        if (strcmp(nom, FIN) != 0) {
            do {
                printf("Note de %s : ", nom);
                scanf("%f", &note);
            } while (note < NOTE_MIN || note > NOTE_MAX);
            ++nbEtudiants;
            // Si l'étudiant a au moins la moyenne
            if (note > (NOTE_MAX - NOTE_MIN) / 2) {
                ++nbEtudiantsAvoirMoyenne;
            }
        }
    } while (strcmp(nom, FIN) != 0);

    if (nbEtudiants == 0) {
        printf("Aucune donnée!\n");
    } else {
        printf("Pourcentage d'étudiants ayant plus de la moyenne: %g%%\n", nbEtudiantsAvoirMoyenne / (float)nbEtudiants * 100);
    }

    return EXIT_SUCCESS;
}