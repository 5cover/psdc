#include <stdio.h>
#include <stdlib.h>

int main()
{
    int a = 0, b = 0, resultat = 0;

    printf("nombre total de gateaux = ");
    scanf("%d", &a);
    printf("gateaux par invité = ");
    scanf("%d", &b);

    for (int i = b; i < a; i += b)
        ++resultat;

    printf("Vous pouvez inviter %d personnes", resultat);

    return EXIT_SUCCESS;
}