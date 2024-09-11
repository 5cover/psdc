/** @file
 * @brief eligibilitePermis
 * @author raphael
 * @date 10/09/2024
 */

#include <stdbool.h>
#include <stdio.h>
#include <stdlib.h>

bool lireBool(void) {
    char c;
    c = 'n';
    scanf("%c", &c);
    return c == 'o';
}

int main() {
    bool estMajeur, aLeCode, aAssezDeLecons, aUnAnDePratique;
    printf("Êtes-vous majeur? (o/n) \n");
    estMajeur = lireBool();
    printf("Avez-vous obtenu le code? (o/n) \n");
    aLeCode = lireBool();
    printf("Avez-vous fait au moins 21h de leçons de conduite? (o/n) \n");
    aAssezDeLecons = lireBool();
    printf("Avez-vous une année de pratique? (o/n) \n");
    aUnAnDePratique = lireBool();
    printf("Droit de passer le permis de conduire : %hhu\n", estMajeur && aLeCode && aAssezDeLecons && aUnAnDePratique);

    return EXIT_SUCCESS;
}
