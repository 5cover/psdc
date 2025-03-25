/*
Ex. 2.1.b TD7 Dév

Modifier la procédure pour saisir une partie seulement des valeurs du tableau ; le nombre nbElt d'éléments qui seront saisis sera un paramètre de la procédure (nbElt <= MAX).

*/

programme tableaux c'est

constante entier MAX := 100;

procédure remplirTableau(sortF tab : tableau[MAX] de entier, entF nbElt : entier) c'est
début
    i : entier;
    pour i de 1 à nbElt pas 1 fairedon
        écrireEcran("Entrer la valeur ", i, " : ");
        lireClavier(tab[i]);
    finfaire
fin