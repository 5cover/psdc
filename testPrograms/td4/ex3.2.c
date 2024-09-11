/**
 * @file
 * @brief Ex. 3.2 TD4 Dév.
 * @author rbardini
 * @date 3/10/2023
 *
 * affiche les tables de multiplications de chaque entier [min;max]
 */

#include <stdio.h>
#include <stdlib.h>

int main()
{
    int min = 0, max = 0;
    int table = 0;
    int facteur = 0;
    do {
        printf("entier min = ");
        scanf("%d", &min);
        printf("entier max = ");
        scanf("%d", &max);
    } while (min >= max);

    table = min;
    while (table < max) {
        printf("\nTable de %d: ", table);
        facteur = 0;
        while (facteur < 9) {
            printf("\t%d", facteur * table);
            ++facteur;
        }
        ++table;
    }

    return EXIT_SUCCESS;
}