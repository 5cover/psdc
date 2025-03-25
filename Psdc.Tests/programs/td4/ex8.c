/**
 * @file
 * @brief Ex. 8 TD4 Dév.
 * @author rbardini
 * @date 5/10/2023
 *
 * Détermine la moyennes des clients d'un magasin ainsi que d'autres informations à partir des valeurs entrées pour chaque jour ouvré.
 */

#include <stdio.h>
#include <stdlib.h>

int const FIN = -5, FERME = -2, CLIENTS_MAX = 100;

int main()
{
    int sommeClients = 0;
    int nbJoursOuvres = 0;
    int nbClients = 0;

    do {
        do {
            printf("Nombre de clients = ");
            scanf("%d", &nbClients);
        } while ((nbClients < 0 || nbClients > CLIENTS_MAX) && (nbClients != FIN && nbClients != FERME));
        if (nbClients != FIN && nbClients != FERME) {
            ++nbJoursOuvres;
            sommeClients += nbClients;
        }
    } while (nbClients != FIN);

    printf("Somme totale des clients = %d\n", sommeClients);
    printf("Nombre de jours ouvrés = %d\n", nbJoursOuvres);
    if (nbJoursOuvres == 0) {
        printf("Moyenne de clients = aucune donnée\n");
    } else {
        printf("Moyenne de clients = %g\n", sommeClients / (float)nbJoursOuvres);
    }

    return EXIT_SUCCESS;
}