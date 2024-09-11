/** @file
 * @brief Ex. 1 TD10
 * @author rbardini
 * @date 28/11/23
 */

#include <stdio.h>
#include <stdlib.h>

#define N 100

typedef int t_tablo[N];

int maxTab(t_tablo const tab)
{
    int i, max;
    max = tab[0];
    for (i = 1; i < N; ++i) {
        if (tab[i] > max) {
            max = tab[i];
        }
    }

    return max;
}

int indMin(t_tablo const tab)
{
    int i, iMin;
    iMin = 0;
    for (i = 1; i < N; ++i) {
        if (tab[i] < tab[iMin]) {
            iMin = i;
        }
    }

    return iMin;
}

void copie(t_tablo const tabIn, t_tablo tabOut)
{
    int i;
    for (i = 0; i < N; ++i) {
        tabOut[i] = tabIn[i];
    }
}

void triRempCroi(t_tablo const tab, t_tablo tabRes)
{
    t_tablo tabTemp;
    copie(tab, tabTemp);

    int const max = maxTab(tabTemp);

    for (int i = 0; i < N; ++i) {
        int iMin = indMin(tabTemp);
        tabRes[i] = tabTemp[iMin];
        tabTemp[iMin] = max;
    }
}

void triRempDec(t_tablo const tab, t_tablo tabRes)
{
    t_tablo tabTemp;
    copie(tab, tabTemp);

    int const max = maxTab(tabTemp);

    for (int i = N; i >= 0; --i) {
        int iMin = indMin(tabTemp);
        tabRes[i] = tabTemp[iMin];
        tabTemp[iMin] = max;
    }
}

// Function to print the elements of an array
void printArray(t_tablo const t)
{
    for (int i = 0; i < N; i++) {
        printf("%d ", t[i]);
    }
    putchar('\n');
}

int main()
{
    t_tablo const arr = {5, 2, 8, 12, 1};
    t_tablo arrRes;

    printf("Original array: ");
    printArray(arr);

    printf("Array sorted in ascending order: ");
    triRempCroi(arr, arrRes);
    printArray(arrRes);

    printf("Array sorted in descending order: ");
    triRempDec(arr, arrRes);
    printArray(arrRes);

    return 0;
}