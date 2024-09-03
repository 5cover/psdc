#include <stdio.h>

typedef struct {
    int a;
    struct { int c; } b;
    struct { struct { int f; } e; } d;
} S;


int main() {
    S s = { 1, 2, 3 };
    printf("%d %d %d\n", s.a, s.b.c, s.d.e.f);
}