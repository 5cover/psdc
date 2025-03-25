/** @file
 * @brief cube
 * @author raphael
 * @date 10/09/2024
 */

#include <stdio.h>
#include <stdlib.h>

int main() {
    float n;
    printf("Entrer un nombre : \n");
    scanf("%g", &n);
    printf("%g => %g\n", n, n * n * n);

    return EXIT_SUCCESS;
}
