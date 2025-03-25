/*
Ex. 2.4.b TD7 Dév

Compléter le programme principal pour tester cette procédure remplace().
*/

programme tableaux c'est

    constante entier MAX := 100;

début
    tab : tableau[MAX] de entier;
    
    remplirTableau(sortE tab, entE 3);

    remplace(entE/sortE tab, entE MAX);

    afficheTableau(entE tab, entE MAX);
fin