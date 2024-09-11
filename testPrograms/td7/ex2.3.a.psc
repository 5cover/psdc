/*
Ex. 2.3.a TD7 Dév

Écrire la fonction fNbFois() comptant, dans le tableau tab donné en paramètre avec son nombre d’éléments réellement remplis, le nombre de valeurs égales à une valeur n également donnée en paramètre.
*/

programme tableaux c'est

fonction fNbFois(entF tab : tableau[] de entier, entF nbElements : entier, entF valeurCherchee : entier) délivre entier c'est
début
    occurences : entier;
    occurences := 0;
    pour (i de 1 à nbElements pas 1) faire
        si (tab[i] == valeurCherche) alors
            occurences := occurences + 1;
        finsi
    finfaire 
fin