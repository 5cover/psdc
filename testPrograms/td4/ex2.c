/**
 * @file
 * @brief Ex. 2 TD4 Dév.
 * @author rbardini
 * @date 3/10/2023
 *
 * Dessine un rectangle
 */

#include <stdio.h>
#include <stdlib.h>

char const CARACTERE = '*';

int main()
{
    int nbCol = 0, nbLig = 0;
    int iCol = 0, iLig = 0;

    do {
        printf("Naturel nbCol = ");
        scanf("%d", &nbCol);
    } while (nbCol < 0);
    do {
        printf("Naturel nbLig = ");
        scanf("%d", &nbLig);
    } while (nbLig < 0);

    while (iCol < nbCol) {
        iLig = 0;
        while (iLig < nbLig) {
            printf("%c", CARACTERE);
            ++iLig;
        }
        printf("\n");
        ++iCol;
    }

    return EXIT_SUCCESS;
}