/** @file
 * @brief CalculPremietre
 * @author raphael
 * @date 10/09/2024
 */

#include <stdio.h>
#include <stdlib.h>

#define PI 3.14159

int main() {
    float r;
    printf("Rayon du cercle : \n");
    scanf("%g", &r);
    printf("Le cercle de rayon %g a un périmètre de %g\n", r, 2 * PI * r);

    return EXIT_SUCCESS;
}
