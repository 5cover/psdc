programme functions c'est

// Naive pow function
fonction pow(entF x: réel, entF i: entier) délivre réel;

procédure printNTimes(entF str: chaine, entF n: entier);

procédure swap(entF/sortF p1 : entier, entF/sortF p2 : entier);

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

procédure swap(entF/sortF p1 : entier, entF/sortF p2 : entier) c'est
début
    tmp : entier;
    tmp := p1;
    p1 := p2;
    p2 := tmp;
fin

début
    printNTimes(entE "Bonjour", entE pow(entE 2, entE 3));
    x : entier;
    x := 0;
    y : entier;

    incrémenter(entE/sortE x);
    swap(entE/sortE x, entE/sortE y);

fin