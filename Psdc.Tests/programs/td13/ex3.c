#include <stdio.h>
#include <time.h>

// compilaton : gcc -Wall -Wextra -pedantic -g -Og ex3.c td12/date/fonctions_date.o -o ex3

#include "td12/date/fonctions_date.h"

int main()
{
    char date[MAX_SIZE];
    time_t temps;

    temps = time(NULL); // date et heure de l'instant

    printf("%ld\n", temps);
    date2str(temps, date);

    printf("%s\n", date);
    temps = date2int(date);

    printf("%ld\n", temps);

    return 0;
}