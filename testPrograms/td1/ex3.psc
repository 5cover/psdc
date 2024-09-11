programme swap c'est début
    a, b, t : caractère;
    écrireEcran("a: ");
    lireClavier(a);
    écrireEcran("b: ");
    lireClavier(b);
    
    t := b;
    b := a;
    a := t;

    écrireEcran("a: ", a);    
    écrireEcran("a: ", b);    
fin