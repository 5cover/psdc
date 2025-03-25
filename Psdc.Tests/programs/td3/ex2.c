#include <stdio.h>
#include <stdlib.h>

int const MIN = 1, MAX = 49;

int main()
{
    int n = 0;

    printf("Entier entre %d et %d (inclusif) : ", MIN, MAX);
    scanf("%d", &n);

    while (n < MIN || n > MAX) {
        if (n < MIN) {
            printf("Trop petit! ");
        } else {
            printf("Trop grand! ");
        }
        printf("Entrez un nouveau nombre: ");
        scanf("%d", &n);
    }

    return EXIT_SUCCESS;
}