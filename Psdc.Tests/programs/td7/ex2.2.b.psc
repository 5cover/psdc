/*
Ex. 2.2.b TD7 Dév

Compléter le programme principal pour tester cette procédure.
*/

programme tableaux c'est

    constante entier MAX := 100;

début
    tab : tableau[MAX] de entier;

    remplirTableau(sortE tab, entE 3);

    écrireEcran("Valeurs du tableau : ");
    afficheTableau(entE tab, entE MAX);
fin