programme ConstantFoldingExample c'est

constante entier C := 10;

procédure proc(entF s : chaîne(10 + C)) c'est
début
    écrireEcran(s);
fin

début
    s1 : chaîne(20);
    s2 : chaîne(10.2 + 11.1);
    s3 : chaîne(10 + C);

    proc(entE s1);
    proc(entE s2);
    proc(entE s3);
    proc(entE "01234567890123456789");

    n : entier;
    n := 5 / 0;
    n := 5 / (11 - C - 1);
fin