programme ConstantFoldingExample c'est

constante entier C := 10;

procédure proc(entF s1 : chaîne, entF s2 : chaîne) c'est
début
    si s1 == s2 alors
        écrireEcran("oui");
    finsi
fin

début
    s1 : chaîne(20);
    s2 : chaîne(10.2 + 11.1);
    s3 : chaîne(10 + C);

    proc(entE s1, entE s1);
    proc(entE s2, entE s2);
    proc(entE s3, entE s3);
    proc(entE 1);

    b : booléen;
    b := 0.1 == 0.2;
    b := 0.1 + 0.2 == 0.3; // uh oh
    n : entier;
    n := 5 / 0;
    n := 5 / (11 - C - 1);
fin