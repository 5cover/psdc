/*
Ex. 2.9 TD7 Dév

Proposer une procédure pour rechercher et effacer toutes les valeurs 0 du tableau tab, déplacer les éléments restants pour ne pas laisser de cases vides et mettre à jour le nombre d'éléments effectifs dans le tableau.
*/

programme tableaux c'est

procédure supprimerZéros(entF/sortF tab : tableau[] de entier, entF/sortF nbElements : entier) c'est
début
    // Pour chaque élement :
    //      Si il est égal à 0
    //          Décrémenter nbElements
    //          Pour cet élement et chaque élément qui le suit
    //              Assigner à l'élément suivant
    i : entier;
    j : entier;

    pour (i de 1 à nbElements pas 1) faire
        si (tab[i] == 0) alors
            nbElements := nbElements - 1;
            pour (j de i à nbElements pas 1) faire
                tab[j] := tab[j + 1];
            finfaire
        finsi
    finfaire
fin