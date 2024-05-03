programme StructureExamples c'est

type t_personne = structure
début
    c_prenom: chaîne(50);
    c_nom: chaîne(50);
    c_estUnHomme: booléen;
    c_age: entier;
fin;

début
    p : t_personne;
    p.c_prenom := "Scover";
    p.c_nom := "NoLastName";
    p.c_estUnHomme := vrai;
    p.c_age := 300;

    struct.arr[i] := 0;
    arr[i].struct := 0;
fin