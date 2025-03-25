#include <stdio.h>
#include <stdlib.h>

/**
 * @brief Ex. 7 TD3 Dév.
 * @author rbardini
 * @date 27/09/2023
 *
 * Multiplication additive
 */

int main()
{
    int a = 0, b = 0, resultat = 0;

    printf("a = ");
    scanf("%d", &a);
    printf("b = ");
    scanf("%d", &b);

    while (a > 0) {
        resultat += b;
        --a;
    }

    printf("a * b = %d", resultat);

    return EXIT_SUCCESS;
}