#include <stdbool.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

// déclaration des constantes symboliques

#define MAX_MESSAGES 20
#define MAX_CAR 80

// définition des types

typedef char t_message[MAX_CAR];

typedef struct {
    t_message message;
} t_element;

typedef struct {
    t_element tabElt[MAX_MESSAGES];
    size_t nb;
} t_file;

// definition des constantes

t_element ELTVIDE = (t_element){
    .message = "------ce message est vide-------",
};

// prototypes des fonctions

t_file initialiser(void);
bool enfiler(t_file *file, t_element nouvelElt);
t_element defiler(t_file *file);
void vider(t_file *file);
t_element tete(t_file const *file);
bool estVide(t_file const *file);
bool estPleine(t_file const *file);

void afficherElement(t_element e);
t_element saisieElement(void);

void supprimer_trop_anciens(t_file *file, int nbASupprimer);
void sauvegardeFichier(t_file const *file, char const *nomFichier);
void lectureFichier(t_file *file, char const *nomFichier);

// secret.. chuuut
void afficherTous(t_file file);

void saisirNomFichier(char const *mode, t_message nomFichier);

// programme principal
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
            enfiler(&maFile, saisieElement());
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
        } break;
        case 10: { // lecture des messages du fichier texte
            t_message nomFichier;
            saisirNomFichier("r", nomFichier);
            lectureFichier(&maFile, nomFichier);
        } break;
        default: printf("erreur de saisie\n");
        }
    } while (choix != 0);
    return EXIT_SUCCESS;
}

// Definitions des fonctions

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
        printf("Message %zu. ", i + 1);
        afficherElement(file.tabElt[i]);
    }
}

void afficherElement(t_element e)
{
    printf("%s\n", e.message);
}

t_element saisieElement(void)
{
    t_element elt;
    printf("saisir un nouveau message : ");
    fgets(elt.message, MAX_CAR, stdin);          // vider le buffeur de caractères (si nécessaire)
    fgets(elt.message, MAX_CAR, stdin);          // saisie d'une chaine de caractères sécurisée
    elt.message[strlen(elt.message) - 1] = '\0'; // suppression du caractère ‘\n’ de validation de fin de saisie

    return elt;
}

void supprimer_trop_anciens(t_file *file, int nbASupprimer)
{
    for (int i = 0; i < nbASupprimer; ++i) {
        defiler(file);
    }
}

void sauvegardeFichier(t_file const *file, char const *nomFichier)
{
    FILE *fichier = fopen(nomFichier, "w");

    for (size_t i = 0; i < file->nb; ++i) {
        fprintf(fichier, "%s\n", file->tabElt[i].message);
    }

    fclose(fichier);
}

void lectureFichier(t_file *file, char const *nomFichier)
{
    FILE *fichier = fopen(nomFichier, "r");

    t_element e;
    while (fgets(e.message, MAX_MESSAGES, fichier) != NULL) {
        enfiler(file, e);
    }

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