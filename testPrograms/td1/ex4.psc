programme moyenne c'est
début
    a, b : entier;
    moyenne : réel;
    écrireEcran("Entrer un nombre : ");
    lireClavier(a);
    écrireEcran("Entrer un deuxième nombre : ");
    lireClavier(b);

    moyenne := (a + b) / 2.0;
    
    écrireEcran("La moyenne entre ", a, " et ", b, " est ", moyenne);
fin