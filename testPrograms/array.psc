programme StructureExamples c'est

type tNotes = tableau[20] de réel;

début
    notes : tNotes;

    notes[1] := 5.4;
    notes[2] := 12.7;
    notes[19] := 19.99;

    écrireEcran(notes[19]);
fin