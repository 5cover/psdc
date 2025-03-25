/*
Ex.1 TD7 Dév.

Proposer une procédure (ou une fonction ?) écrite en langage algorithmique qui affiche la 
table de multiplication (de 0 à 9) de l'entier n, n étant fourni en paramètre. Que vaut l'indice de boucle en fin de 
procédure/fonction ?

En fin de procédure l'indice vaut n.
*/

programme tableDeMultiplication c'est

procédure afficherTableDeMultiplication(entF n : entier) c'est
début
    i : entier;
    écrireEcran("Table de multiplication de ", n, " : ");
    pour i de 1 à 9 pas 1 faire
        écrireEcran(n, " * ", i, " = ", n*i);
    finfaire
fin

début
    n : entier;
    écrireEcran("n = ");
    lireClavier(n);
    afficherTableDeMultiplication(entE n);
fin

