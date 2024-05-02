/** @file
 * @brief Ex. 1 TD10
 * @author rbardini
 * @date 28/11/23
 */

#include <stdio.h>
#include <stdlib.h>

#define MAX_ELT 10
#define VAL_MAX 99
#define VAL_MIN 0

#define TEMP_LEN (VAL_MAX - VAL_MIN + 1)

typedef int t_tablo[MAX_ELT];
typedef int t_temp[TEMP_LEN];

void triComptage(t_tablo const tab, t_tablo tabRes);
void init(t_temp tab);
void gene(t_tablo tab);

void triComptage(t_tablo const tab, t_tablo tabRes)
{
    t_temp tabTemp;
    init(tabTemp);

    for (int i = 0; i < MAX_ELT; ++i) {
        ++tabTemp[tab[i] - VAL_MIN];
    }

    int cntVal = 0;
    for (int i = 0; i < TEMP_LEN; ++i) {
        while (tabTemp[i] > 0) {
            tabRes[cntVal++] = i;
            --tabTemp[i];
        }
    }
}

void init(t_temp tab)
{
    for (int i = 0; i < TEMP_LEN; ++i) {
        tab[i] = 0;
    }
}

void gene(t_tablo tab)
{
    for (int i = 0; i < MAX_ELT; ++i) {
        tab[i] = rand() % TEMP_LEN + VAL_MIN;
    }
}

// Function to print the elements of an array
void printArray(t_tablo const t)
{
    for (int i = 0; i < MAX_ELT; i++) {
        printf("%d ", t[i]);
    }
    putchar('\n');
}

int main()
{
    t_tablo const arr = {5, 2, 8, 12, 1};
    t_tablo arrRes = {0};

    printf("Original array: ");
    printArray(arr);

    printf("Array sorted: ");
    triComptage(arr, arrRes);
    printArray(arrRes);

    return 0;
}