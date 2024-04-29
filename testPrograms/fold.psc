programme ConstantFoldingExample c'est

constante entier C := 10;

procédure proc(entF s : chaîne(10 + C)) c'est
début
    écrireEcran(s);
fin
    

début
    s1 : chaîne(20);
    s2 : chaîne(10 + 10);
    s3 : chaîne(10 + C);

    proc(entE s1);
    proc(entE s2);
    proc(entE s3);
    proc(entE "01234567890123456789");
fin