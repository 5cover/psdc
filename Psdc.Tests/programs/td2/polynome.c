#include <math.h> // note: compiler avec -lm pour linker la bibliothèque mathématique (voir man sqrt)
#include <stdio.h>
#include <stdlib.h>

int main()
{
    int a = 0, b = 0, c = 0;
    printf("a = ");
    scanf("%d", &a);
    printf("b = ");
    scanf("%d", &b);
    printf("c = ");
    scanf("%d", &c);

    if (a == 0) {
        if (b == 0) {
            if (c == 0) {
                printf("Une infinité de solutions réelles.");
            } else {
                printf("Aucune solution réelle.");
            }
        } else {
            printf("1 solution réelle: %g.", -(c / (float)b));
        }
    } else {
        float delta = b * b - 4 * a * c;
        if (delta < 0) {
            printf("Aucune solution réelle.");
        } else if (delta == 0) {
            printf("1 solution réelle: %g.", -b / (2.0 * a));
        } else {
            float x1 = (-b - sqrtf(delta)) / (2 * a);
            float x2 = (-b + sqrtf(delta)) / (2 * a);

            if (x1 == x2) {
                printf("Une solution réelle double: %g.", x1);
            } else {
                printf("Deux solution réelles: %g et %g.", x1, x2);
            }
        }
    }

    return EXIT_SUCCESS;
}