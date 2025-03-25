/** @file
 * @brief définition du programme principal
 * @author rbardini
 * @date 19/12/2023
*/

#include <stdio.h>

#include "fonctions.h"

int main()
{
    // Declaration des variables
    t_file maFile;
    t_element elt;
    t_message msg;
    int choix;
    // initialisation
    maFile = initialiser();
    // ajouter quelques elements
    for (int i = 0; i < 4; i++) {
        sprintf(elt.message, "message %d", i);
        date2str(time(NULL), elt.date);
        enfiler(&maFile, elt);
    }
    do { // menu
        printf("----------------------------------------------------\n");
        printf("0 : quitter\n");
        printf("1 : afficher le nombre d'elements dans la file ?\n");
        printf("2 : ajouter un element a la file\n");
        printf("3 : retirer un element et afficher le message \n");
        printf("4 : afficher le message de la tete de file\n");
        printf("5 : vider la file\n");
        printf("6 : la file est-elle vide ?\n");
        printf("7 : la file est-elle pleine ?\n");
        printf("8 : supprimer les messages trop anciens\n");
        printf("9 : sauvegarde dans un fichier texte et vider\n");
        printf("10: lecture des messages du fichier texte\n");
        printf("11: supprimer les messages antérieurs à une date\n");
        printf("votre choix : ");
        scanf("%d", &choix);
        printf("----------------------------------------------------\n");
        // traitement
        switch (choix) {
        case -1: afficherTous(maFile);
        case 0: break;
        case 1: // afficher le nombre d'elements dans la file
            printf("La file contient %zu éléments.\n", maFile.nb);
            break;
        case 2: { // ajouter un element (à donner aux étudiants)
            if (!enfiler(&maFile, saisieElement())) {
                fprintf(stderr, "Impossible d'ajouter car la pile est pleine.\n");
            };
        } break;
        case 3: // retirer un element et afficher le message
            afficherElement(defiler(&maFile));
            break;
        case 4: // afficher le message de la tete de file
            afficherElement(tete(&maFile));
            break;
        case 5: // vider la file
            vider(&maFile);
            break;
        case 6: // la file est-elle vide ?
            printf("La file %s vide\n", estVide(&maFile) ? "EST" : "N'EST PAS");
            break;
        case 7: // la file est-elle pleine ?
            printf("La file %s pleine\n", estPleine(&maFile) ? "EST": "N'EST PAS");
            break;
        case 8: { // supprimer les messages trop anciens
            int nbASupprimer;
            printf("Nombre d'éléments à supprimer : ");
            while (scanf("%d", &nbASupprimer) != 1 || nbASupprimer < 0)
                ;
            supprimer_trop_anciens(&maFile, nbASupprimer);
        } break;
        case 9: { // sauvegarde dans un fichier texte et vider
            t_message nomFichier;
            saisirNomFichier("w", nomFichier);
            sauvegardeFichier(&maFile, nomFichier);
            vider(&maFile);
        } break;
        case 10: { // lecture des messages du fichier texte
            t_message nomFichier;
            saisirNomFichier("r", nomFichier);
            lectureFichier(&maFile, nomFichier);
        } break;
        case 11: {
            time_t unixSecondsDateMin;
            printf("Date min (secondes unix) : ");
            while (scanf("%ld", &unixSecondsDateMin) != 1)
                ;
            supprimer_anciens_date(&maFile, unixSecondsDateMin);
        } break;
        default: printf("erreur de saisie\n");
        }
    } while (choix != 0);
    return EXIT_SUCCESS;
}