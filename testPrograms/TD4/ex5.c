/**
 * @file
 * @brief Ex. 5 TD4 Dév.
 * @author rbardini
 * @date 5/10/2023
 *
 * détermine le maximum d'une séquence de nombres terminée par -1.
 */

#include <stdio.h>
#include <stdlib.h>

int const FIN = -1;

int main()
{
    int maximum = FIN;
    int valeur = 0;

    do {
        printf("Entier positif = ");
        scanf("%d", &valeur);
        if (valeur > 0 && valeur > maximum && valeur != FIN) {
            maximum = valeur;
        }
    } while (valeur != FIN);

    if (maximum == FIN) {
        printf("Pas de maximum\n");
    } else {
        printf("Maximum = %d\n", maximum);
    }

    return EXIT_SUCCESS;
}