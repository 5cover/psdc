programme ex2 c'est

constante entier MAX_ELT := 10;
constante entier VAL_MAX := 99;
constante entier VAL_MIN := 0;
type t_tablo = tableau[MAX_ELT] de entier;
type t_tabTemp = tableau[VAL_MIN..VAL_MAX] de entier;

/*
Tri par comptage manuel :
12 10 12 6 4
Maximum : 12
1  0
2  0
3  0
4  1
5  0
6  1
7  0
8  0
9  0
10 1
11 0
12 2

Tableau trié :
4 6 10 12 12
*/

procédure triComptage(entF tab : t_tablo, sortF tabRes : t_tablo) c'est début
    tabTemp : t_tabTemp;
    i : entier;

    // étape 1 : les valeurs du tableau tabTemp sont initialisées à 0
    init(sortE tabTemp);

    // étape 2 : le tableau tab est parcouru et chaque valeur tabTemp[tab[i]] est incrémentée
    pour i de 1 à MAX_ELT faire
        tabTemp[tab[i]] := tabTemp[tab[i]] + 1;
    finfaire

    // étape 3 : on parcourt le tableau tabTemp pour construire le tableau tabRes résultat
    /*
        int cntVal = 0;
    for (int i = 0; i < TEMP_LEN; ++i) {
        while (tabTemp[i] > 0) {
            tabRes[cntVal++] = i;
            --tabTemp[i];
        }
    }*/
fin

procédure init(sortF tab : t_tabTemp) c'est
début
    pour i de VAL_MIN à VAL_MAX faire
        tab[i] := 0;
    finfaire
fin