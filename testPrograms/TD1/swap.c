#include <stdio.h>
#include <stdlib.h>

int main()
{
    float a = 0, b = 0, tmp = 0;

    printf("Entrer un nombre : ");
    scanf("%f", &a);
    printf("Entrer un nombre : ");
    scanf("%f", &b);

    printf("a vaut %g, b vaut %g\n", a, b);

    tmp = a;
    a = b;
    b = tmp;

    printf("a vaut %g, b vaut %g\n", a, b);

    return EXIT_SUCCESS;
}