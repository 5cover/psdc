/**
 * @file
 * @brief Ex. 1 TD4 Dév.
 * @author rbardini
 * @date 3/10/2023
 *
 * Calcul d'une puissance entière
 */

#include <stdio.h>
#include <stdlib.h>

int main()
{
    float x = 0;
    int n = 0;
    float resultat = 1;
    int i = 0;

    printf("réel x = ");
    scanf("%f", &x);
    do {
        printf("entier n = ");
        scanf("%d", &n);
    } while (n < 0);

    i = 0;
    while (i < n) {
        ++i;
        resultat *= x;
    }

    printf("%g puissance %d = %g\n", x, n, resultat);
    return EXIT_SUCCESS;
}