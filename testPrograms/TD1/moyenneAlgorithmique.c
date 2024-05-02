#include <stdio.h>
#include <stdlib.h>

int main()
{
    float noteDS1 = 0, noteDS2 = 0, noteTP = 0;

    printf("Note DS1 : ");
    scanf("%f", &noteDS1);
    printf("Note DS2 : ");
    scanf("%f", &noteDS2);
    printf("Note TP : ");
    scanf("%f", &noteTP);

    float moyenneDS = (noteDS1 + noteDS2 * 3) / 4;
    float moyenneGenerale = moyenneDS * (2 / 3.0) + noteTP * (1 / 3.0);

    printf("La moyenne de DS est de : %g, la moyenne générale est de %g.\n", moyenneDS, moyenneGenerale);

    return EXIT_SUCCESS;
}