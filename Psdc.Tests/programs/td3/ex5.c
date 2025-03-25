#include <stdbool.h>
#include <stdio.h>
#include <stdlib.h>

/**
 * @brief Ex. 5 TD3 Dév.
 * @author rbardini
 * @version 1.0
 * @date 26/09/2023
 *
 * description
 */

float const CARTE1_RMAX = 800, CARTE2_RMAX = 1500;

int main()
{
    float retraitMax = 0, montant = 0;

    // J'ai eu quelques problèmes vis-à-vis du scanf pour Visa Premier (scanf ne read qu'un seul mot), donc j'ai mis un menu à la place.
    printf("1. Visa\n2. Visa Premier\nType de carte : ");
    bool erreur = false;
    do {
        int carte = 0;
        scanf("%d", &carte);
        switch (carte) {
        case 1:
            retraitMax = CARTE1_RMAX;
            break;
        case 2:
            retraitMax = CARTE2_RMAX;
            break;
        default:
            erreur = true;
            break;
        }
    } while (erreur);

    printf("Retrait maximum : %g€\nEntrer 0 pour arrêter\n", retraitMax);
    do {
        printf("Montant : ");
        scanf("%f", &montant);
        if (montant < 0) {
            printf("Le montant ne peut pas être négatif.\n");
        } else if (montant > retraitMax) {
            printf("Le montant est supérieur au maximum.\n");
        } else if (montant != 0) {
            printf("%g€ débités\n", montant);
        }
    } while (montant != 0);

    return EXIT_SUCCESS;
}