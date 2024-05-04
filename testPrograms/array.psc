programme StructureExamples c'est

type tNotes = tableau[20] de réel;

début
    notes : tNotes;

    notes[0] := 0; // error: index out of bounds
    notes[1] := 5.4;
    notes[2] := 12.7;
    notes[19] := 19.99;
    notes[20] := 0;

    écrireEcran(notes[20]);
fin