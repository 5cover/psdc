#include <stdbool.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

// 2.a les constantes symboliques
#define MAX_ANNONCES 1000
#define MAX_LIGNES 100
#define MAX_CHAMP 81
#define SCN_CHAMP "%81s"

// 2.b les types

typedef char t_lignes[MAX_LIGNES];
typedef char t_champ[MAX_CHAMP + 1];

typedef struct {
    t_champ marque;
    t_champ designation;
    int id;
    int annee;
    int km;
    int prix;
} t_annonce;

typedef struct {
    t_annonce annonces[MAX_ANNONCES];
    int nb;
} t_stock;

// 3.a les prototypes des fonctions

/// @brief Initialisation du stock (toutes les annonces seront initialisées)
void initStock(t_stock *stock);
/// @brief Lecture du fichier texte avec suppression des caractères espace dans les champs
bool lectureTexte1(t_stock *stock, FILE *fichier);
/// @brief Lecture du fichier texte sans suppression des caractères espace dans les champs
bool lectureTexte2(t_stock *stock, FILE *fichier);
/// @brief Lecture du fichier texte avec définition de variables locales pour le tableau des annonces
bool lectureTexte3(t_stock *stock, FILE *fichier);
/// @brief Affichage des annonces
void afficheAnnonces(t_stock const *stock);
/// @brief Affichage des annonces d’une marque spécifique
void afficheAnnoncesMarque(t_stock const *stock, t_champ marque);
/// @brief Sauvegarde des annonces dans un fichier binaire
void sauvegardeBinaire(t_stock const *stock, FILE *fichier);
/// @brief Lecture des annonces depuis le fichier binaire
void lectureBinaire(t_stock *stock, FILE *fichier);
/// @brief Saisie d’une nouvelle annonce
t_annonce saisieAnnonce(void);
/// @brief Ajout une nouvelle annonce dans le fichier binaire
void ajoutAnnonceFichierBinaire(t_annonce annonce, FILE *fichier);

void saisirChamp(char const *prompt, t_champ champ);
bool lireChampTexte(FILE *fichier, t_champ resultat);
bool lireChampEntier(FILE *fichier, int *resultat);
bool lireChampTexteIgnorerEspaces(FILE *fichier, t_champ resultat);

// les constantes

// 3.c
t_annonce const ANNONCE_VIDE = (t_annonce){
    .marque = "",
    .designation = "",
    .id = 0,
    .annee = 0,
    .km = 0,
    .prix = 0,
};

// le programme principal
int main()
{
    t_stock stock_auto;
    int choix = -1;
    // initialisation des structures
    initStock(&stock_auto);
    // menu
    while (choix != 0) {
        printf("---------------------------------------------------------------------\n");
        printf("1 : lire le fichier texte des annonces (pas d'espace dans les champs)\n");
        printf("2 : lire le fichier texte des annonces \n");
        printf("3 : lire le fichier texte des annonces (variables locales)\n");
        printf("4 : afficher les annonces\n");
        printf("5 : afficher les annonces d'une marque\n");
        printf("6 : sauvegarder les annonces dans un fichier binaire\n");
        printf("7 : lire le fichier binaire des annonces\n");
        printf("8 : ajouter une annonce dans le fichier binaire\n");
        printf("9 : supprimer une annonce\n");
        printf("votre choix :");
        scanf("%d", &choix);
        switch (choix) {
        case 0: break;
        case 1: {
            t_champ nomFichier;
            saisirChamp("Nom du fichier : ", nomFichier);
            FILE *fichier = fopen(nomFichier, "r");
            if (fichier) {
                lectureTexte1(&stock_auto, fichier);
                fclose(fichier);
            } else {
                perror(nomFichier);
            }
        } break;
        case 2: {
            t_champ nomFichier;
            saisirChamp("Nom du fichier : ", nomFichier);
            FILE *fichier = fopen(nomFichier, "r");
            if (fichier) {
                lectureTexte2(&stock_auto, fichier);
                fclose(fichier);
            } else {
                perror(nomFichier);
            }
        } break;
        case 3: {
            t_champ nomFichier;
            saisirChamp("Nom du fichier : ", nomFichier);
            FILE *fichier = fopen(nomFichier, "r");
            if (fichier) {
                lectureTexte3(&stock_auto, fichier);
                fclose(fichier);
            } else {
                perror(nomFichier);
            }
        } break;
        case 4:
            afficheAnnonces(&stock_auto);
            break;
        case 5: {
            t_champ nomMarque;
            saisirChamp("Nom de la marque : ", nomMarque);
            afficheAnnoncesMarque(&stock_auto, nomMarque);
        } break;
        case 6: {
            t_champ nomFichier;
            saisirChamp("Nom du fichier binaire : ", nomFichier);
            FILE *fichier = fopen(nomFichier, "w");
            if (fichier) {
                sauvegardeBinaire(&stock_auto, fichier);
                fclose(fichier);
            } else {
                perror(nomFichier);
            }
        } break;
        case 7: {
            t_champ nomFichier;
            saisirChamp("Nom du fichier binaire : ", nomFichier);
            FILE *fichier = fopen(nomFichier, "r");
            if (fichier) {
                lectureBinaire(&stock_auto, fichier);
                fclose(fichier);
            } else {
                perror(nomFichier);
            }
        } break;
        case 8: {
            t_champ nomFichier;
            saisirChamp("Nom du fichier binaire : ", nomFichier);
            FILE *fichier = fopen(nomFichier, "a");
            if (fichier) {
                puts("Annonce :");
                t_annonce a = saisieAnnonce();

                ajoutAnnonceFichierBinaire(a, fichier);
                fclose(fichier);
            } else {
                perror(nomFichier);
            }
        } break;
        default: printf("erreur de choix\n");
        }
    }
    return EXIT_SUCCESS;
}

void initStock(t_stock *stock)
{
    stock->nb = 0;
    for (int i = 0; i < MAX_ANNONCES; ++i) {
        stock->annonces[i] = ANNONCE_VIDE;
    }
}

bool lectureTexte1(t_stock *stock, FILE *fichier)
{
    bool ok = true;

    stock->nb = 0;

    // skip la 1re ligne
    while (getc(fichier) != '\n')
        ;

    while (ok && stock->nb < MAX_ANNONCES) {
        t_annonce *a = &stock->annonces[stock->nb++];

        ok = lireChampEntier(fichier, &a->id) && lireChampTexteIgnorerEspaces(fichier, a->marque) && lireChampTexteIgnorerEspaces(fichier, a->designation) && lireChampEntier(fichier, &a->annee) && lireChampEntier(fichier, &a->km) && lireChampEntier(fichier, &a->prix);
    }

    return ok;
}

bool lectureTexte2(t_stock *stock, FILE *fichier)
{
    bool ok = true;

    stock->nb = 0;

    // skip la 1re ligne
    while (getc(fichier) != '\n')
        ;

    while (ok && stock->nb < MAX_ANNONCES) {
        t_annonce *a = &stock->annonces[stock->nb++];

        ok = lireChampEntier(fichier, &a->id) && lireChampTexte(fichier, a->marque) && lireChampTexte(fichier, a->designation) && lireChampEntier(fichier, &a->annee) && lireChampEntier(fichier, &a->km) && lireChampEntier(fichier, &a->prix);
    }

    return ok;
}

bool lectureTexte3(t_stock *stock, FILE *fichier)
{
    t_stock stock2;
    bool ok = lectureTexte2(&stock2, fichier);

    if (ok) {
        stock->nb = stock2.nb;
        for (int i = 0; i < stock2.nb; ++i) {
            stock->annonces[i] = stock2.annonces[i];
        }
    }

    return ok;
}

void afficheAnnonces(t_stock const *stock)
{
    puts("id, marque, designation, annee, km, prix");
    for (int i = 0; i < stock->nb; ++i) {
        t_annonce a = stock->annonces[i];
        printf("%d, %s, %s, %d, %d, %d\n",
               a.id, a.marque, a.designation, a.annee, a.km, a.prix);
    }
}

void afficheAnnoncesMarque(t_stock const *stock, t_champ marque)
{
    puts("id, marque, designation, annee, km, prix");
    for (int i = 0; i < stock->nb; ++i) {
        t_annonce a = stock->annonces[i];
        if (strcmp(a.marque, marque) == 0) {
            printf("%d, %s, %s, %d, %d, %d\n",
                   a.id, a.marque, a.designation, a.annee, a.km, a.prix);
        }
    }
}

void sauvegardeBinaire(t_stock const *stock, FILE *fichier)
{
    if (fwrite(stock->annonces, sizeof *stock->annonces, stock->nb, fichier) != (size_t)stock->nb) {
        fprintf(stderr, "[erreur] de sauvegarde binaire\n");
    }
}

void lectureBinaire(t_stock *stock, FILE *fichier)
{
    int nbLus = 0;
    while (fread(&stock->annonces[nbLus], sizeof *stock->annonces, 1, fichier) != 0) {
        ++nbLus;
    }

    stock->nb = nbLus;
}

t_annonce saisieAnnonce(void)
{
    t_annonce a;
    bool ok;
    do {
        ok = (printf("id          : "), scanf("%d", &a.id)) == 1;
        ok &= (printf("marque      : "), scanf("%19s", a.marque)) == 1;
        ok &= (printf("designation : "), scanf("%19s", a.designation)) == 1;
        ok &= (printf("annee       : "), scanf("%d", &a.annee)) == 1;
        ok &= (printf("km          : "), scanf("%d", &a.km)) == 1;
        ok &= (printf("prix        : "), scanf("%d", &a.prix)) == 1;
    } while (!ok && a.annee < 0 || a.km < 0 || a.prix < 0);
    return a;
}

void ajoutAnnonceFichierBinaire(t_annonce annonce, FILE *fichier)
{
    fseek(fichier, 0, SEEK_END);
    fwrite(&annonce, sizeof annonce, 1, fichier);
}

// autres fonctions

void saisirChamp(char const *prompt, t_champ champ)
{
    puts(prompt);
    scanf(SCN_CHAMP, champ);
}

bool lireChampEntier(FILE *fichier, int *resultat)
{
    return fscanf(fichier, "%d\t", resultat) == 1;
}

bool lireChampTexte(FILE *fichier, t_champ resultat)
{
    int i = 0;
    int c = getc(fichier);
    while (i <= MAX_CHAMP && c != EOF && c != '\t') {
        resultat[i++] = (char)c;
        c = getc(fichier);
    }

    return i <= MAX_CHAMP + 1;
}

bool lireChampTexteIgnorerEspaces(FILE *fichier, t_champ resultat)
{
    int i = 0;
    int c = getc(fichier);
    while (i <= MAX_CHAMP && c != EOF && c != '\t') {
        if (c != ' ') {
            resultat[i++] = (char)c;
        }
        c = getc(fichier);
    }

    return i <= MAX_CHAMP + 1;
}
