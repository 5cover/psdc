#include <stdbool.h>
#include <stdio.h>
#include <stdlib.h>

void lireBool(bool *);

int main()
{
    bool estMajeur = false, aLeCode = false, aAssezDeLecons = false, aUnAnDePratique = false;

    printf("Êtes-vous majeur? (o/n) ");
    lireBool(&estMajeur);
    printf("Avez-vous obtenu le code? (o/n) ");
    lireBool(&aLeCode);
    printf("Avez-vous fait au moins 21h de leçons de conduite? (o/n) ");
    lireBool(&aAssezDeLecons);
    printf("Avez-vous une année de pratique? (o/n) ");
    lireBool(&aUnAnDePratique);

    printf("Vous %s le droit de passer le permis de conduire.\n", estMajeur && aLeCode && aAssezDeLecons && aUnAnDePratique ? "avez" : "n'avez pas");

    return EXIT_SUCCESS;
}

void lireBool(bool *b)
{
    char c = 'n';
    scanf("%c", &c);
    getchar(); // Lire le '\n' en trop.
    *b = c == 'o';
}