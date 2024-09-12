/*
Ex. 2.2.a TD7 Dév

Écrire une procédure afficheTableau() utilisant une boucle "pour", afin d'afficher à l'écran 
toutes les valeurs existantes du tableau tab.
*/

programme tableaux c'est

procédure afficheTableau(entF tab : tableau[] de entier, entF nbElt : entier) c'est
début
    i : entier;
    pour (i de 1 à nbElt pas 1) faire
        écrireEcran("[", i, "]", tab[i]);
    finfaire
fin