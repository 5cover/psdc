//#eval expr "im before u guys"

programme CompilerDirectives c'est

type tNotes = tableau[6] de réel;
#eval type tNotes

constante entier TEN := 10;
#assert TEN == 11 "Apologies."

type A = structure début
    i: entier;
#eval expr "im in a struct"
fin;

//#eval expr "im at da top lvl"

début
    x: A := {
//#eval expr "im in an initializer"
    };

//#eval expr "im a stmt"

    pour x.i de 1 à 10 pas 2 faire
        écrireEcran("hello");
#eval expr x
#eval expr x.i
    finfaire
fin

//#eval expr "im after u guys"