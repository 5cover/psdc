/**
 * @file
 * @brief Ex. 7 TD4 Dév.
 * @author rbardini
 * @date 5/10/2023
 *
 * Calcule la somme 1 + 1/2 + 1/3 + ... + 1/n.
 */

#include <stdio.h>
#include <stdlib.h>

int main()
{
    int n = 0;
    int i = 1;
    float resultat = 1;

    do {
        printf("n = ");
        scanf("%d", &n);
    } while (n < 1);

    while (i < n) {
        ++i;
        resultat += (1.0 / i);
    }

    printf("%g\n", resultat);

    return EXIT_SUCCESS;
}