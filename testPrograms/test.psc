programme StructureExamples c'est

type tNotes = tableau[6] de réel;

type A = structure début
    i: entier;
    @evaluateExpr("im in a struct")
fin

@evaluateExpr("im at da top lvl")

début
    x: A := {
        @evaluateExpr("im in an initializer")
    };

    @evaluateExpr("im a stmt")
    
    pour x.i de 1 à 10 pas 2 faire
        écrireEcran("a
        aaaa
        aa");
    finfaire

    @evaluateType(tNotes)
fin