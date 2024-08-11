programme StructureExamples c'est

type tNotes = tableau[6] de réel;

début
    notes : tNotes;

    notes[0, 1] := 0; // error: index out of bounds
    notes[1] := 5.4;
    notes[2] := 12.7;
    notes[6] := -1;
    notes[7] := -2;

    e : entier;

    notes2 : tNotes := {
        1,
        [6,6] := 2,
        3,
    };

    écrireEcran(notes[6]);
fin