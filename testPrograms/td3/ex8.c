#include <stdio.h>
#include <stdlib.h>

/**
 * @brief Ex. 8 TD3 Dév.
 * @author rbardini
 * @date 27/09/2023
 *
 * Impressions
 */

#define COUT_PAR_PAGE 0.1

int main()
{
    int totalPages = 0;
    int nbImpressions = 0;

    printf("Nombre d'impressions: ");
    scanf("%d", &nbImpressions);

    for (int i = 1; i <= nbImpressions; ++i) {
        int nbPages = 0;
        printf("Nombre de pages de l'impression %d : ", i);
        scanf("%d", &nbPages);
        totalPages += nbPages;
    }

    float moyennePages = 0;
    if (nbImpressions > 0) {
        moyennePages = totalPages / (float)nbImpressions;
    }
    printf("Un total de %d pages ont été imprimées pour un coût total de %g€ (%g "
           "pages par impression en moyenne).\n",
           totalPages, totalPages * COUT_PAR_PAGE, moyennePages);

    return EXIT_SUCCESS;
}