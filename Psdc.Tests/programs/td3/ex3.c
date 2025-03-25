#include <stdio.h>
#include <stdlib.h>

int const FIN = -1;

int main()
{
    int somme = 0;
    int nbValeurs = 0;
    int n = 0;
    printf("Entrer une suite de nombres terminée par %d\n", FIN);
    do {
        printf("Entrez un nombre : ");
        scanf("%d", &n);
        if (n != FIN) {
            if (n < 0) {
                printf("Les valeurs négatives ne sont PAS autorisées\n");
            } else {
                somme += n;
                ++nbValeurs;
            }
        }
    } while (n != FIN);

    printf("Vous avez entré %d valeurs dont la somme est %d\n", nbValeurs, somme);

    return EXIT_SUCCESS;
}