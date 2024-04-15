// Naive pow function
fonction pow(entF x: réel, entF i: entier) délivre réel;
procédure printNTimes(entF str: chaine, entF n: entier);

fonction pow(entF x: réel, entF i: entier) délivre réel c'est début
    res : entier;
    res := 1;

    tant que (i > 0) faire
        res := res * res;
        i := i - 1;
    finfaire

    retourne res;
fin

procédure printNTimes(entF str: chaine, entF n: entier) c'est début
    i : entier;
    j : entier;
    pour i de 0 à n faire
        écrireEcran(str);
    finfaire
fin

procédure incrémenter(entF/sortF n: entier) c'est début
    n := n + 1;
fin

programme functions c'est début
    printNTimes(entE "Bonjour", entE pow(entE 2, entE 3));
    n : entier;
    n := 0;
    incrémenter(entE/sortE n);
fin