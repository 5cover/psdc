#include <stdio.h>
#include <stdlib.h>

float const FRANCHISE_MIN = 150;
float const FRANCHISE_MAX = 600;

int main()
{
    float montantSinistre = 0;

    printf("Montant du siniste ? ");
    scanf("%f", &montantSinistre);

    float montantFranchise = montantSinistre / 10;
    if (montantFranchise < FRANCHISE_MIN) {
        montantFranchise = FRANCHISE_MIN;
    } else if (montantFranchise > FRANCHISE_MAX) {
        montantFranchise = FRANCHISE_MAX;
    }
    printf("Montant remboursé: %g", montantSinistre - montantFranchise);
    return EXIT_SUCCESS;
}