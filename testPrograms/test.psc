programme functions c'est

procédure printNTimes(entF str: chaine, entF n: entier);

procédure printNTimes(entF str: chaine, entF n: entier) c'est début
    i : entier;
    j : entier;
    pour i de 0 à n faire
        écrireEcran(str);
    finfaire
fin

procédure sayHello() c'est
début
    écrireEcran("hello");
fin

procédure incrémenter(entF/sortF n: entier) c'est début
    n := n + 1;
fin

début
    printNTimes(entE "Bonjour", entE 1);
fin