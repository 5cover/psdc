/**
 * @file
 * @brief Ex. 3.1 TD4 Dév.
 * @author rbardini
 * @date 3/10/2023
 *
 * affiche la table de multiplication d'un entier [0;9]
 */

#include <stdio.h>
#include <stdlib.h>

int main()
{
    int i = 0;
    int n = 0;

    do {
        printf("Entier [0;9] = ");
        scanf("%d", &n);
    } while (n < 0 || n > 9);

    while (i < 10) {
        printf("\t%d", n * i);
        ++i;
    }

    return EXIT_SUCCESS;
}