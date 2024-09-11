/*
Ex. 2.4.a TD7 Dév

Écrire la procédure remplace() permettant de remplacer dans le tableau tab, donné en paramètre avec son nombre d'éléments, toutes les valeurs 0 par des 1 et de transmettre le tableau modifié au 
programme appelant.
*/

programme tableaux c'est

procédure remplace(entF/sortF tab : tableau[] de entier, entF nbElements : entier) c'est
début
    pour (i de 1 à nbElements pas 1) faire
        si (tab[i] == 0) alors
            tab[i] := 1;
        finsi
    finfaire
fin

