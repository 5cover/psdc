#eval expr "im before u guys"

programme CompilerDirectives c'est

type tNotes = tableau[6] de réel;

type A = structure début
    i: entier;
#eval expr "im in a struct"
fin;

#eval expr "im at da top lvl"

début
    x: A := {
#eval expr "im in an initializer"
    };

#eval expr "im a stmt"
    
    pour x.i de 1 à 10 pas 2 faire
        écrireEcran("a
        aaaa
        aa");
    finfaire

#eval type tNotes
fin

#eval expr "im after u guys"