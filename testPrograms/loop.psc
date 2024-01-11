programme Compteur c'est
début
    écrireEcran("Programme compteur");
    
    start, end, step : entier;

    // Program showcasing the 4 kinds of loops

    // while
    start := -1;
    tant que (start < 0) faire
        écrireEcran("début = ");
        lireClavier(start);
    finfaire

    // do ... until
    répéter
        écrireEcran("fin = ");
        lireClavier(end);
    jusqu'à (end >= 0)
    
    // do ... while
    faire
        écrireEcran("pas = ");
        lireClavier(step);
    tant que (step == 0)

    // for
    i : entier;
    pour i de 0 à 9 pas 1 faire
        écrireEcran(i);
    finfaire
    
fin