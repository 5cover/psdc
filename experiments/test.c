#include <stdio.h>

int main()
{
    #define S 10
    struct { int x; int v[S]; } a = { 5 };
    printf("%d\n", a.x);
    for (int i = 0; i < S; ++i) {
        printf("%d ", a.v[i]);
    }

    return 0;
}