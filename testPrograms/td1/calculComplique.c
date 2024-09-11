#include <stdio.h>
#include <stdlib.h>

int main()
{
    float
        a = 0,
        b = 0,
        c = 0,
        d = 0,
        x = 0,
        x1 = 0,
        x2 = 0,
        x11 = 0,
        x21 = 0,
        x111 = 0,
        x112 = 0,
        x211 = 0,
        x212 = 0,
        x2111 = 0;

    printf("a = ");
    scanf("%f", &a);
    printf("b = ");
    scanf("%f", &b);
    printf("c = ");
    scanf("%f", &c);
    printf("d = ");
    scanf("%f", &d);

    x2111 = c / d;
    x212 = a + b;
    x211 = 1 - x2111;
    x112 = b / 2;
    x111 = a * a * a;
    x21 = x211 + x212;
    x11 = x111 + x112;
    x2 = 3.14 + x21;
    x1 = x11 * x11;
    x = x1 / x2;

    printf("\nx = %f8\n", x);

    return EXIT_SUCCESS;
}