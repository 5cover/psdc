/**
 * @file
 * @brief Ex. 4 TD4 Dév.
 * @author rbardini
 * @date 3/10/2023
 *
 * calcule la moyenne de plusieurs notes
 */

#include <stdio.h>
#include <stdlib.h>
#define A int
A const NOTE_MIN = 0;
float const NOTE_MAX = 20;

int main()
{
    float sommeNotes = 0;
    float note = 0;
    int nbNotes = 0;
    int i = 1;

    do {
        printf("nbNotes = ");
        scanf("%d", &nbNotes);
    } while (nbNotes < 0);

    while (i <= nbNotes) {
        do {
            printf("Note %d = ", i);
            scanf("%f", &note);
        } while (note < NOTE_MIN || note > NOTE_MAX);
        sommeNotes += note;
        ++i;
    }
    printf("Moyenne = %g\n", sommeNotes / nbNotes);

    return EXIT_SUCCESS;
}