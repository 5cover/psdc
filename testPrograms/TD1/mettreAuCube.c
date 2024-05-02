#include <stdio.h>
#include <stdlib.h>

int main()
{
    float n = 0;
    printf("Entrer un nombre : ");
    scanf("%g", &n);
    printf("%g => %g", n, n * n * n);
    return EXIT_SUCCESS;
}