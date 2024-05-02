/*
Ex. 2.6.b TD7 Dév


Compléter le programme principal précédent pour tester cette fonction existe().
*/

programme tableaux c'est

    constante entier MAX := 100;

début
    tab : tableau[MAX] de entier;
    trouve2 : booléen;

    remplirTableau(sortE tab, entE 3);

    trouve2 := existe(entE tab, entE MAX, entE 2);

    si (trouve2) alors
        écrireEcran("2 existe dans le tableau.");
    sinon
        écrireEcran("2 n'existe pas dans le tableau.");
    finsi
fin