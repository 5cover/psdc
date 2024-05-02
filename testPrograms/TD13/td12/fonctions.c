/** @file
 * @brief définitions des fonctions
 * @author rbardini
 * @date 19/12/2023
*/

#include <stdio.h>
#include <string.h>

#include "fonctions.h"
#include "const.h"
#include "globales.h"

t_file initialiser(void)
{
    t_file file = (t_file){.nb = 0};
    for (int i = 0; i < MAX_MESSAGES; ++i) {
        file.tabElt[i] = ELTVIDE;
    }

    return file;
}

bool enfiler(t_file *file, t_element nouvelElt)
{
    bool possible = !estPleine(file);

    if (possible) {
        file->tabElt[file->nb++] = nouvelElt;
    }

    return possible;
}

t_element defiler(t_file *file)
{
    bool possible = !estVide(file);

    if (possible) {
        t_element premier = file->tabElt[0];
        for (size_t i = 1; i < file->nb; ++i) {
            file->tabElt[i - 1] = file->tabElt[i];
        }
        --file->nb;
        return premier;
    }

    return ELTVIDE;
}

void vider(t_file *file)
{
    file->nb = 0;
    *file = initialiser();
}

t_element tete(t_file const *file)
{
    bool possible = !estVide(file);

    if (possible) {
        return file->tabElt[file->nb - 1];
    }

    return ELTVIDE;
}

bool estVide(t_file const *file)
{
    return file->nb == 0;
}

bool estPleine(t_file const *file)
{
    return file->nb == MAX_MESSAGES;
}

void afficherTous(t_file file)
{
    for (size_t i = 0; i < file.nb; ++i) {
        printf("Msg %zu : ", i + 1);
        afficherElement(file.tabElt[i]);
    }
}

void afficherElement(t_element e)
{
    printf("%s %s\n", e.date, e.message);
}

t_element saisieElement(void)
{
    t_element elt;
    printf("saisir un nouveau message : ");
    fgets(elt.message, MAX_CAR, stdin);          // vider le buffeur de caractères (si nécessaire)
    fgets(elt.message, MAX_CAR, stdin);          // saisie d'une chaine de caractères sécurisée
    elt.message[strlen(elt.message) - 1] = '\0'; // suppression du caractère ‘\n’ de validation de fin de saisie

    date2str(time(NULL), elt.date); // la date sera la date actuelle

    return elt;
}

void supprimer_trop_anciens(t_file *file, int nbASupprimer)
{
    for (int i = 0; i < nbASupprimer; ++i) {
        defiler(file);
    }
}

void supprimer_anciens_date(t_file *file, time_t unixSecondsDateMin)
{
    for (size_t i = 0; i < file->nb; ++i) {
        if (date2int(file->tabElt[i].date) < unixSecondsDateMin) {
            for (size_t iSuiv = i; iSuiv < file->nb - 1; ++iSuiv) {
                file->tabElt[i] = file->tabElt[i + 1];
            }
            --file->nb;
            --i; // on recule d'un cran dans l'itération car on a complé le 'trou' laissé par l'élément supprimé avec l'élément suivant.
        }
    }
}

void sauvegardeFichier(t_file const *file, char const *nomFichier)
{
    FILE *fichier = fopen(nomFichier, "w");

    for (size_t i = 0; i < file->nb; ++i) {
        fprintf(fichier, "%s %s\n", file->tabElt[i].date, file->tabElt[i].message);
    }

    fclose(fichier);
}

void lectureFichier(t_file *file, char const *nomFichier)
{
    FILE *fichier = fopen(nomFichier, "r");

    t_element e;
    while (fscanf(fichier, "%s %[^\n]\n", e.date, e.message) == 2 && enfiler(file, e))
        ;

    fclose(fichier);
}

void saisirNomFichier(char const *mode, t_message nomFichier)
{
    printf("Nom du fichier : ");
    scanf("%79s", nomFichier);

    FILE *fichier;
    while ((fichier = fopen(nomFichier, mode)) == NULL) {
        printf("Ce fichier ne peut pas être ouvert\n");
        scanf("%79s", nomFichier);
    }
    fclose(fichier);
}