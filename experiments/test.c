#include <stdio.h>

void f(int [const]);

int main() {
    int a[10];
    f(a);
}

void f(int arr[const]) {
    arr[5] = 5;
}
