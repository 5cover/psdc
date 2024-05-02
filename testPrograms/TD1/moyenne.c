#include <stdio.h>
#include <stdlib.h>

int main()
{
    int a = 0, b = 0;

    printf("Entrer un nombre : ");
    scanf("%d", &a);
    printf("Entrer un deuxième nombre : ");
    scanf("%d", &b);

    float moyenne = (a + b) / 2.0;

    printf("La moyenne entre %d et %d est %g\n", a, b, moyenne);

    return EXIT_SUCCESS;
}
