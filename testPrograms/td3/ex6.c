#include <stdio.h>
#include <stdlib.h>

/**
 * @brief Ex. 6 TD3 Dév.
 * @author rbardini
 * @date 27/09/2023
 *
 * Factorielle
 */

int main()
{
    int n = 0;
    int resultat = 1;

    printf("n = ");
    scanf("%d", &n);

    if (n < 0) {
        printf("n ne doit pas être négatif\n");
    } else if (n == 0) {
        printf("n! = 0\n");
    } else {
        while (n > 0) {
            resultat = resultat * n;
            --n;
        }
        printf("n! = %d\n", resultat);
    }
    return EXIT_SUCCESS;
}