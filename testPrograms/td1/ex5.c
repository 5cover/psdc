/** @file
 * @brief moyenneAlgorithmique
 * @author raphael
 * @date 10/09/2024
 */

#include <stdio.h>
#include <stdlib.h>

int main() {
    float noteDS1, noteDS2, noteTP, moyenneDS, moyenneGenerale;
    printf("Note DS1 : \n");
    scanf("%g", &noteDS1);
    printf("Note DS2 : \n");
    scanf("%g", &noteDS2);
    printf("Note TP : \n");
    scanf("%g", &noteTP);
    moyenneDS = (noteDS1 + noteDS2 * 3) / 4;
    moyenneGenerale = moyenneDS * (2 / 3.0) + noteTP * (1 / 3.0);
    printf("La moyenne de DS est de : %g la moyenne générale est de %g.\n", moyenneDS, moyenneGenerale);

    return EXIT_SUCCESS;
}
