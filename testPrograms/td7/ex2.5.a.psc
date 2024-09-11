/*
Ex. 2.5.a TD7 Dév

On propose la procédure incrEntier() permettant d’incrémenter un entier de 1 unité. Appeler 
cette procédure dans le programme principal de manière à incrémenter le troisième élément du tableau tab.
*/

programme tableaux c'est

    constante entier MAX := 100;

début
    tab : tableau[MAX] de entier;
    
    remplirTableau(sortE tab, entE 3);

    incrEntier(entE/sortE tab[3]);

    afficheTableau(entE tab, entE MAX);
fin