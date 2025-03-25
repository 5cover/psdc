#include <stdio.h>
#include <stdlib.h>

int main()
{
    char nom[50];
    int categorie = 0;
    int nbNuits = 0;
    printf("Nom ? ");
    scanf("%s", nom);
    printf("Catégorie ? ");
    scanf("%d", &categorie);
    printf("Nombre de nuits ? ");
    scanf("%d", &nbNuits);

    float plafond = 0;
    switch (categorie) {
    case 1:
        plafond = 40;
        break;
    case 2:
        plafond = 55.5;
        break;
    case 3:
        plafond = 70;
        break;
        // faut-il mettre un default?
    }

    printf("%s, votre plafond de remboursement s'élève à %g€.", nom, plafond * nbNuits);

    return EXIT_SUCCESS;
}