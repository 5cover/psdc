#include <stdio.h>
#include <stdlib.h>

int main()
{
    int anciennete = 0;
    float salaire = 0;
    char nom[50];

    printf("Quel est votre nom? ");
    scanf("%s", nom);

    printf("Quel est votre salaire? ");
    scanf("%f", &salaire);

    printf("Quelle est votre ancienneté en années? ");
    scanf("%d", &anciennete);

    // Raisonnement par actions
    int numeroPrime = 0;
    float montantPrime = 0;
    if (anciennete < 10 || salaire >= 10000) {
        numeroPrime = 1;
        montantPrime = 1000;
    } else {
        numeroPrime = 2;
        montantPrime = 2000;
    }
    printf("%s, vous avez le droit à la prime %d (%g€).");
    return EXIT_SUCCESS;
}