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

    char min = 0;
    if (a < b) {
        if (a < c) {
            min = 'a';
        } else {
            min = 'c';
        }
    } else {
        if (b < c) {
            min = 'b';
        } else {
            min = 'c';
        }
    }

    printf("Le minimum est %c.", min);
    return EXIT_SUCCESS;
}