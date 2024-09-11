/*
Ex. 2.1.c TD7 Dév

Proposer un programme pour tester la procédure remplirTableau().
*/

programme tableaux c'est

    constante entier MAX := 100;

début
    tab : tableau[MAX] de entier;

    remplirTableau(sortE tab, entE 3);

    écrireEcran("Valeurs du tableau : ");
    i : entier;
    pour (i de 1 à MAX pas 1) faire
        écrireEcran("[", i, "] = ", tab[i]);
    finfaire
fin