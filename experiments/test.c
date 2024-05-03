/** @file
 * @brief tableDeMultiplication
 * @author raphael
 * @date 01/05/2024
 */

#include <stdio.h>

typedef struct {
    int i;
} S;

int main() {
    S s = {5};
    printf("i = \n", s.(i));
}