#include <stdio.h>
#include <stdlib.h>

int main()
{
    float const PI = 3.14159;
    float r = 0;

    printf("Rayon du cercle : ");
    scanf("%f", &r);

    printf("Le cercle de rayon %g a un périmètre de %g", r, 2 * PI * r);

    return EXIT_SUCCESS;
}