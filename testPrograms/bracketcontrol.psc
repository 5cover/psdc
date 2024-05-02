programme Brackets c'est
// Support for non-standard brackets in control structures
// This is a parser test
début
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
    tant que 1 faire
    finfaire

    tant que (1) faire
    finfaire

    // do..while
    faire
    tant que 1

    faire
    tant que (1)

    // repeat
    répéter
    jusqu'à (1)

    répéter
    jusqu'à 1
fin