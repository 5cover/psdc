programme ex1 c'est

constante entier N := 100;
type t_tablo = tableau[N] de entier;

/*
Exemple de tri par remplacement manuel:
8 0 9 3 5   | 0 0 0 0 0
8 [0] 9 3 5 | 0 0 0 0 0
8 0 9 [3] 5 | 0 3 0 0 0
8 0 9 3 [5] | 0 3 5 0 0
[8] 0 9 3 5 | 0 3 5 8 0
8 0 [9] 3 5 | 0 3 5 8 9

Le tri est croissant.
*/

fonction maxTab(entF tab : t_tablo) délivre entier c'est début
    i, max : entier;
    max := tab[1];
    pour i de 2 à N faire début
        si (tab[i] > max) alors
            max := tab[i];
        finsi
    finfaire

    retourne max;
fin

fonction indMin(entF tab : t_tablo) délivre entier c'est début
    i, iMin : entier;
    iMin := 1;
    pour i de 2 à N faire début
        si (tab[i] < tab[iMin]) alors
            iMin := i;
        finsi
    finfaire

    retourne iMin;
fin

procédure copie(entF tabIn : t_tablo, sortF tabOut : t_tablo) c'est début
    i : entier;
    pour i de 1 à N faire début
        tabOut[i] := tabIn[i];
    finfaire
fin

procédure triRempCroi(entF tab : t_tablo, sortF tabRes : t_tablo) c'est début
    i, iMin, max : entier;

    max := maxTab(entE tab);

    pour (i de 1 à N) faire début
        // recherche du minimum
        iMin := indMin(entE tab);
        tabRes[i] := tab[iMin];
        // on remplace le min par le max pour ne pas le reprendre
        tab[iMin] := max;
    finfaire
fin

procédure triRempDec(entF tab : t_tablo, sortF tabRes : t_tablo) c'est début
    i, iMin, max : entier;

    max := maxTab(entE tab);

    pour (i de 1 à N) faire début
        // recherche du minimum
        iMin := indMin(entE tab);
        tabRes[N - i] := tab[iMin];
        // on remplace le min par le max pour ne pas le reprendre
        tab[iMin] := max;
    finfaire
fin