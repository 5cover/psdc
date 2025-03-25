/*
Ex 1 TD9 dév.
*/

constante entier NB_LIG := 5;
constante entier NB_COL := 10;

type t_tablo = tableau[NB_LIG][NB_COL] de entier;

/*
1. Proposez une procédure pour saisir toutes les valeurs d'un tableau de type type t_tablo
*/
procédure saisir(sortF tabl : t_tablo) c'est
début
    i, j : entier;
    pour i de 1 à NB_LIG faire
        pour j de 1 à NB_COL faire
            écrireEcran("Valeur ligne ", i, " colonne ", j, " : ");
            lireClavier(tabl[i][j]);
        finfaire
    finfaire
fin

/*
2. Proposez une procédure pour l'affichage d'un tableau de type t_tablo
*/
procédure afficher(entF tabl : t_tablo) c'est
début  
    i, j : entier;  
    pour i de 1 à NB_LIG faire
        pour j de 1 à NB_COL faire
            écrireEcran("Valeur ligne ", i, " colonne ", j, " : ", tabl[i][j]);
        finfaire
    finfaire
fin

/*
3. Proposez un programme permettant de tester ces procédures
*/
programme ex1 c'est
début
    tabl : t_tablo;
    écrireEcran("Sasie du tableau");
    saisir(sortE tabl);
    écrireEcran("Affichage du tableau");
    saisir(entE tabl);
fin