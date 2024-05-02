programme ForwardDeclarations c'est

fonction estPair(entF n : entier) délivre booléen;
fonction estImpair(entF n : entier) délivre booléen;

fonction double(entF n : entier) délivre entier;

procédure dire(entF quoi : chaîne);

début
    i : entier;
    b : booléen;

    b := estPair(entE 17);

    i := double(entE 34);

    dire(entE "bonjour");
fin

fonction double(entF n : entier) délivre entier c'est début
    retourne n * 2;
fin

fonction estPair(entF n : entier) délivre booléen c'est début
    si (n == 0) alors
        retourne vrai;
    finsi
    retourne estImpair(entE n - 1);
fin

fonction estImpair(entF n : entier) délivre booléen c'est début
    si (n == 0) faire
        retourne faux;
    finsi
    retourne estPair(entE n - 1);
fin

procédure dire(entF quoi : chaîne) c'est début
    écrireEcran("Je dis : ", quoi);
fin