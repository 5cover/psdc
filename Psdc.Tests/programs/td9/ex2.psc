/*
Ex 2 TD9 dév.
*/

constante entier NB := 10;
type t_matrice_carree = tableau[NB, NB] de entier;

/*
1. Proposez une fonction délivrant la somme des éléments de la diagonale "Nord Ouest - Sud Est" d'un tableau de type t_matrice_carree
*/
fonction matrice_somme_diagonaleNOSE(entF matrice : t_matrice_carree) délivre entier c'est
début
    entier somme, i;
    somme := 0;
    
    // On assume que le coin Nord Ouest correspond à la 1re ligne, 1re colonne
    pour i de 1 à NB faire
        somme := somme + matrice[i, i]
    finfaire

    retourne somme;
fin

/*
2. Proposez une procédure qui affiche le triangle supérieur gauche d'un tableau de type t_matrice_carree
*/
procédure matrice_afficher_triangle_supGauche(entF matrice : t_matrice_carree) c'est
début
    entier i, j;
    // Assume que écrireÉcran ne fait pas de nouvelle ligne
    pour i de 1 à NB faire
        pour j de 1 à (NB - i + 1) faire
            écrireEcran(matrice[i, j], " ");
        finfaire
        écrireEcran("\n");
    finfaire
fin

/*
3. Proposez une procéure pour transposer une matrice contenue dans un tableau de type t_matrice_carree (sans utiliser d'autre tableaux)
*/
procédure matrice_transposer(entF/sortF matrice : t_matrice_carree) c'est
début
    // Transposer une matrice carrée : faire le symétrique par l'axe de la diagonale NO SE
    // (1,2) = (2,1)
    // (5,7) = (7,5)

    // exemple:
    // (1,2)
    // (3,4)
    // =
    // (1,3)
    // (2,4)

    i, j, tmp : entier;
    pour i de 1 à NB faire
        pour j de 1 à NB faire
            tmp := matrice[i,j];
            matrice[i,j] = matrice[j,i];
            matrice[j,i] := tmp;
        finfaire
    finfaire
fin
