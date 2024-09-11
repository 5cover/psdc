/*
Ex. 3 TD7 Dév
*/

programme ex3 c'est

constante entier MAX := 100;
type typeTab = tableau[MAX] de entier;

procédure initTab(sortF tab : typeTab) c'est
début
    entier i;
    pour i de 0 à MAX faire
        tab[i] := 0;
    finfaire
fin

fonction saisieEntPos() délivre entier c'est
début
    entier saisi;

    faire
        écrireEcran("Entrer un entier positif : ");
        lireClavier(saisi);
    tant que (saisi <= 0);

    retourne saisi;
fin

procédure insereEntTab(entF/sortF tab : typeTab, entF valeur) c'est
début
    indiceInsertion : entier;
    i : entier;

    indiceInsertion := 1;

    // Si on atteint la fin du tableau, on a pas trouvé d'emplacement correct, la valeur sera insérée à la dernière case du tableau.
    tant que (indiceInsertion < MAX ET tab[indiceInsertion] >= valeur) faire
        indiceInsertion := indiceInsertion + 1;
    finfaire

    i := MAX - 1;
    tant que (i >= indiceInsertion) faire
        tab[i + 1] := tab[i];
        i := i - 1;
    finfaire

    tab[indiceInsertion] := valeur;
fin

procédure afficheTab(entF tab) c'est
début
    i : entier;
    pour i de 1 à MAX faire
        écrireEcran("[", i, "] = ", tab[i]);
    finfaire
fin

début
    i : entier;
    val : entier;
    tab : typeTab;

    initTab(sortE tab);

    pour i de 1 à MAX faire
        val := saisieEntPos();
        insereEntTab(entE/sortE tab, entE val);
        afficheTab(entE tab);
    finfaire
fin