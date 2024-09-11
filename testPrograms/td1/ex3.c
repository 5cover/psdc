/** @file
 * @brief swap
 * @author raphael
 * @date 10/09/2024
 */

#include <stdio.h>
#include <stdlib.h>

int main() {
    char a, b, t;
    printf("a: \n");
    scanf("%c", &a);
    printf("b: \n");
    scanf("%c", &b);
    t = b;
    b = a;
    a = t;
    printf("a: %c\n", a);
    printf("a: %c\n", b);

    return EXIT_SUCCESS;
}
