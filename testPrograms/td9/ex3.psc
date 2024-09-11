/*
Ex 3 TD9 dév.
*/

constante entier NB_LIG := 3;
constante entier NB_COL := 4;

type t_ligne = tableau[NB_LIG] de entier;
type t_colonne = tableau[NB_COL] de entier;
type t_matrice = tableau[NB_LIG, NB_COL] de entier;

/*
1. Proposez la procédure qui calcule la somme de chaque ligne et de chaque colonne d'une matrice m.
*/
procédure sommeLigneColonne(entF m : t_matrice, sortF sL : t_ligne, sortF sC : t_colonne) c'est
début
    i, j : entier;

    // Initialiser tout à 0
    pour i de 1 à NB_LIG faire
        sL[i] = 0;
    finfaire
    pour j de 1 à NB_COL faire
        sC[j] = 0;
    finfaire

    pour i de 1 à NB_LIG faire
        pour j de 1 à NB_COL faire
            sL[i] = sL[i] + m[i, j];
            sC[j] = sL[j] + m[i, j];
        finfaire
    finfaire
fin
