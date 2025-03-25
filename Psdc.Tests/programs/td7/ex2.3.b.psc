/*
Ex. 2.3.b TD7 Dév

Compléter le programme principal pour tester cette fonction.
*/

programme tableaux c'est

    constante entier MAX := 100;

début
    tab : tableau[MAX] de entier;
    occurences1 : entier;
    
    remplirTableau(sortE tab, entE 3);

    occurences1 := fNbFois(entE tab, entE 3, entE 1);

    écrireEcran("Vous avez entré 1 ", occurences1, " fois.");
fin