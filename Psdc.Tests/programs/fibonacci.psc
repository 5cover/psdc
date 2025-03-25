programme Fibonacci c'est

fonction fib(entF n : entier) délivre entier c'est
début
    si (n <= 1) alors
        retourne n;
    finsi
    retourne fib(entE n - 2) + fib(entE n - 1);
fin

début
    i : entier;
    pour i de 0 à 20 faire
        écrireEcran(fib(entE i));
    finfaire
fin