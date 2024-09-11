#include <stdio.h>
#include <stdlib.h>

float const TAUX_TAXE = 0.75;
float const SEUIL = 500;

int main()
{
    float somme = 0;
    float resu = 0;

    printf("Somme? ");
    scanf("%f", &somme);

    if (somme < SEUIL) {
        resu = somme * (TAUX_TAXE / 2);
    } else {
        resu = somme * TAUX_TAXE;
    }

    printf("Résultat : %g", resu);

    return EXIT_SUCCESS;
}