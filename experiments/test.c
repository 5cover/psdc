/** @file
 * @brief ex1
 * @author raphael
 * @date 28/07/2024
 */

#include <stdio.h>
#include <string.h>

#define MAXNUM 3
#define MAXPERS 10

typedef int t_tabNumeros[MAXNUM];
typedef struct {
    char c_nom[31];
    t_tabNumeros c_liste;
    int c_nbComptes;
} t_personne;
typedef t_personne t_tabPersonnes[MAXPERS];

void permuter(t_personne *p1, t_personne *p2)
{
    t_personne tmp;
    // error: pointer need stars
    tmp = *p1;
    *p1 = *p2;
    *p2 = tmp;
}

void afficherPersonne(t_personne p)
{
    printf("%s\n", p.c_nom);
    printf("Comptes : %d\n", p.c_nbComptes);
    int i;
    for (i = 1; i <= p.c_nbComptes; i++) {
        printf("%d : %d\n", i, p.c_liste[i - 1]);
    }
}

void triNaif(t_tabPersonnes *t, int nbPers)
{
    int iMin;
    int i;
    for (i = 1; i <= nbPers; i++) {
        iMin = i;
        int iRecMin;
        for (iRecMin = i + 1; iRecMin <= nbPers; iRecMin++) {
            // error: pointer need stars (with brackets here for operator precedence)
            if (strcmp((*t)[iRecMin - 1].c_nom, (*t)[iMin - 1].c_nom) < 0) {
                iMin = iRecMin;
            }
        }
        permuter(&t[i - 1], &t[iMin - 1]);
    }
}

void afficherTableau(t_tabPersonnes t, int nbPers)
{
    int i;
    for (i = 1; i <= nbPers; i++) {
        afficherPersonne(t[i - 1]);
    }
}

int main()
{
    t_tabPersonnes BANQUE;
    t_personne p;
    // error: can't reassign arrays, such as strings
    // p.c_nom = "toto";
    strcpy(p.c_nom, "toto");
    p.c_nbComptes = 3;
    p.c_liste[0] = 21;
    p.c_liste[1] = 25;
    p.c_liste[2] = 12;
    BANQUE[0] = p;
    strcpy(p.c_nom, "dupont");
    p.c_nbComptes = 1;
    p.c_liste[0] = 56;
    BANQUE[1] = p;
    strcpy(p.c_nom, "albert");
    p.c_nbComptes = 3;
    p.c_liste[0] = 19;
    p.c_liste[1] = 123;
    p.c_liste[2] = 111;
    BANQUE[2] = p;
    strcpy(p.c_nom, "alfred");
    p.c_nbComptes = 2;
    p.c_liste[0] = 20;
    p.c_liste[1] = 321;
    BANQUE[3] = p;
    t_tabPersonnes banque = {
        {
            "toto",
            {
                21,
                25,
                12,
            },
            3,
        },
        {
            "dupont",
            {
                56,
            },
            1,
        },
        {
            "albert",
            {
                19,
                123,
                111,
            },
            3,
        },
        {
            "alfred",
            {
                20,
                312,
            },
            2,
        },
    };
    int i, nb;
    nb = 4;
    for (i = 1; i <= nb; i++) {
        banque[i - 1] = BANQUE[i - 1];
    }
    afficherTableau(banque, nb);
    triNaif(&banque, nb);
    afficherTableau(banque, nb);

    return 0;
}
