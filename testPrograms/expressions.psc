programme Expressions c'est
// arbitrary complex expressions that make use of associativity and precedence rules.
début
    écrireEcran(3 + 4.5 * (7 - 2) / 2.0 >= 'a' + "hello" == (vrai OU faux));
    écrireEcran(-5 + 3 * 6.2 - 1.5 / 2 >= (10.0 + 2.5) * (4.0 - 1.5));
    écrireEcran('c' > 'a' ET 'b' < 'd' OU 5 != 3 + 2 * (7 - 4));
    écrireEcran(NON (5 > 3) OU (2 + 4 == 6 ET 7 < 9));
    écrireEcran((10.0 + 2.5) * (4.0 - 1.5) / 2.0 - (3 + 4.5 * 7 - 2) / 2.0 >= 5.0);
    écrireEcran('z' XOR 'a' > 'm' ET (3 + 4.5 * (7 - 2) / 2.0 >= 'a' + "hello"));
    écrireEcran((NON vrai) ET (faux OU vrai) == (5 > 3 ET 2 + 4 == 6));
    écrireEcran((5 + 3 * 6.2 - 1.5 / 2) >= (10.0 + 2.5) * (4.0 - 1.5) / 2.0);
    écrireEcran('a' + "hello" == (vrai OU faux) ET 3 + 4.5 * (7 - 2) / 2.0 >= 5.0);
    écrireEcran((3 + 4.5 * 7 - 2) / 2.0 >= 'a' + "hello" == (vrai OU faux) OU 5 != 3 + 2 * (7 - 4));


    //écrireEcran(arrayExpr[2, 3] + structExpr.member1 * (func(entE 3.0, entE vrai) - 7) / 2.0);
    //écrireEcran(func(entE arrayExpr[1, 5 - 2], entE s.member2) >= 'a' + "string" == (vrai OU faux));
    //écrireEcran(NON (structExpr.member3 > 3) OU (2 + arrayExpr[0, 4 * 2] == 6 ET 7 < 9));
    //écrireEcran(func(func(entE 3.5, structExpr.member4), arrayExpr[1, 2]) * (4.0 - 1.5) / 2.0 - (3 + 4.5 * 7 - 2) / 2.0 >= 5.0);
    //écrireEcran(arrayExpr[3, 'c' XOR 'a'] > 'm' ET (3 + structExpr.member5 * (7 - 2) / 2.0 >= func(entE 5.0, entE "hello")));
    //écrireEcran(structExpr.member6 + func(entE arrayExpr[0, 7], entE 2.5) == (vrai OU faux) ET 3 + 4.5 * (7 - 2) / 2.0 >= 5.0);
    //écrireEcran((NON vrai) ET (faux OU vrai) == (structExpr.member7 > 3 ET 2 + arrayExpr[1, 1] == 6));
    //écrireEcran((5 + 3 * func(entE 6.2, entE structExpr.member8) - 1.5 / 2) >= (10.0 + arrayExpr[2, 3]) * (4.0 - 1.5) / 2.0);
    //écrireEcran('a' + func(entE "hello", entE structExpr.member9) == (vrai OU faux) OU 3 + arrayExpr[1, 4] * (7 - 2) / 2.0 >= 5.0);
    //écrireEcran((3 + 4.5 * arrayExpr[2, 1] - 2) / 2.0 >= 'a' + func(entE "hello", entE vrai) == (vrai OU faux) OU 5 != 3 + 2 * (structExpr.member10 - 4));

fin