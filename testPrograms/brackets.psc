programme Brackets c'est
// Support for non-standard brackets in control structures
// This is a parser test

constante entier MAX := 10;

début
    i : entier;
    // for loop
    pour (i de 1 à MAX pas 5) faire
    finfaire

    pour i de 1 à MAX pas 5 faire
    finfaire
    

    // if/elsif
    si (1 == 0) alors
    sinonsi (1 == 1) alors
    sinonsi 1 == (0) alors
    finsi
    
    si (1) == 1 alors
    finsi

    // while
    tant que (booléen)1 faire
    finfaire

    tant que ((booléen)1) faire
    finfaire

    // do..while
    faire
    tant que (booléen)1

    faire
    tant que ((booléen)1)

    // repeat
    répéter
    jusqu'à ((booléen)1)

    répéter
    jusqu'à (booléen)1
fin