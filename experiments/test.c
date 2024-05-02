/** @file
 * @brief tableDeMultiplication
 * @author raphael
 * @date 01/05/2024
 */

#include <stdio.h>

void afficherTableDeMultiplication(int n) {
    int i;
    printf("Table de multiplication de %d : \n", n);
    for (i = 1; i <= 9; i += 1) {
        printf("%d * %d = %d\n", n, i, n * i);
    }
}

int main() {
    int n;
    printf("n = \n");
    scanf("%d", &n);
    afficherTableDeMultiplication(n);
}