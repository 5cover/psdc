programme Compteur c'est
début
    écrireEcran("Counter");
    
    start, end, step : entier;

    // Program showcasing the 4 kinds of loops

    // while
    start := -1;
    tant que (start < 0) faire
        écrireEcran("Enter start (>= 0)");
        lireClavier(start);
    finfaire

    // do ... until
    répéter
        écrireEcran("Enter end (>= 0)");
        lireClavier(end);
    jusqu'à (end >= 0)
    
    // do ... while
    faire
        écrireEcran("Enter step (!= 0)");
        lireClavier(step);
    tant que (step == 0)

    si step > 0 et start > end
    ou step < 0 et start < end alors
        écrireEcran("Invalid values");
    sinon
        // for
        écrireEcran("Result");
        i : entier;
        pour i de start à end pas step faire
            écrireEcran(i);
        finfaire
        écrireEcran("Final value: ", i);
    finsi
    
fin