/*
Ex. 2.8 TD7 Dév
Proposer une procédure pour rechercher la plus grande valeur du tableau tab, ainsi que sa plus petite valeur et la moyenne de l'ensemble des valeurs.
*/

programme tableaux c'est

procédure obtenirExtremums(entF tab : tableau[] de entier, entF nbElements : entier, sortF minimum : entier, sortF maximum : entier, sortF moyenne : réel) c'est
début
    i : entier;
    somme : entier;

    somme := tab[1];
    minimum := tab[1];
    maximum := tab[1];

    pour (i de 2 à nbElements pas 1) faire
        somme := somme + tab[i];
        si (tab[i] < minimum) alors
            minimum := tab[i];
        sinonsi (tab[i] > maximum) alors
            maximum := tab[i];
        finsi
    
    moyenne := somme * 1.0 / nbElements;
fin