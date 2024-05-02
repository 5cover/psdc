
/* 1. Proposer une procédure récursive inverse() qui lit une suite d’entiers (terminée par –1) au clavier et les
affiche dans l’ordre inverse de l’ordre dans lequel ils ont été saisis. */
procédure inverse() c'est début
    n : entier;

    écrireEcran("Valeur : ");
    lireClavier(n);
    si (n != -1) alors
        inverse();
        écrireEcran(n);
    finsi
fin

/*
2. Écrire et tester une fonction  qui délivre le PGCD de deux entiers naturels en appliquant l’algorithme d’Euclide :

- si n1>n2 PGCD(n1, n2) = PGCD(n1-n2, n2)  
- si n2>n1 PGCD(n1,n2)  = PGCD(n1, n2-n1)  
*/

/*a. Question : Proposez une solution itérative avec une boucle tant que. La boucle s’arrête quand 
n1 et n2 deviennent égaux.*/
fonction pgcd_iteratif(entF n1 : entier, entF n2 : entier) c'est début
    tant que (n1 != n2) faire
        si (n1 > n2) alors
            n1 := n1 - n2;
        sinon
            n2 := n2 - n1;
        finsi
    finfaire

    retourne n1;
fin

/*b. Question : Proposez une solution récursive.*/
fonction pgcd_recursion(entF n1 : entier, entF n2 : entier) c'est début
    si (n1 == n2) alors
        retourne n1;
    sinonsi (n1 > n2) alors
        retourne pgcd_recursion(n1 - n2, n2);
    sinon
        retourne pgcd_recursion(n1, n2 - n1);
    finsi
fin

/*
3. Récursivité croisée :

Soient les suites (un) et (vn) définies par :
u0 = 1
u1 = vn-1 + 2un-1
v0 = 1
vn = 4vn-1 + un-1

*/

/*a. Calculer u3 et v3 « à la main ».*/

/*
u1 = v0 + 2u0 = 1 + 2 = 3
u2 = v1 + 2u1 = 1 + 2*3 = 7
u3 = v2 + 2u2 = 1 + 2*7 = 15

v1 = 4v0 + u0 = 4 + 1 = 5
v2 = 4v1 + u1 = 4*5 + 3 = 23
v3 = 4v2 + u2 = 4*23 + 7 = 103
*/

/*b. Écrire une procédure récursive qui permet de calculer un et vn.*/
procédure suite(sortF un : entier, sortF vn: entier, entF n : entier) c'est début
    si (n == 0) alors
        un := 1;
        vn := 1;
    sinon
        suite(un, vn, n - 1);
        un := vn + 2*un;
        vn := 4*vn + un;
    finsi
fin