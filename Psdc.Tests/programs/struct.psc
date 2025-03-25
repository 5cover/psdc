programme StructureExamples c'est

type t_personne = structure
début
    c_prenom: chaîne(50);
    c_nom: chaîne(50);
    c_estUnHomme: booléen;
    c_age: entier;
fin;

type tPoint = structure début x, y : réel; fin;
//constante tPoint C_POINT := { 3.14, 14.3 };
//constante tPoint C_POINT_SOME_DES := { .x := 3.14, 14.3 };
//constante tPoint C_POINT_ONLY_DES := { .x := 3.14, .y := 14.3 };

début
    a : tPoint := { .x := 3.14, .y := 14.3 };

    p : t_personne;
    p.c_prenom := "Scover";
    p.c_nom := "NoLastName";
    p.c_estUnHomme := vrai;
    p.c_age := 300;

    // Structure initializer
    p2 : t_personne := {
        "Scover",
        "NoLastName"
    };

    //struct.arr[i] := 0;
    //arr[i].struct := 0;
fin