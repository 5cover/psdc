/** @file
 * @brief moyenne
 * @author raphael
 * @date 10/09/2024
 */

#include <stdio.h>
#include <stdlib.h>

int main() {
    int a, b;
    float moyenne;
    printf("Entrer un nombre : \n");
    scanf("%d", &a);
    printf("Entrer un deuxième nombre : \n");
    scanf("%d", &b);
    moyenne = (a + b) / 2.0;
    printf("La moyenne entre %d et %d est %g\n", a, b, moyenne);

    return EXIT_SUCCESS;
}
