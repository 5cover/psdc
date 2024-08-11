programme Variables c'est
// Showcasing different variable types and their formatting

début
    // Basic
    bol  : booléen;
    bol  := vrai;
    s : chaîne;

    file : nomFichierLog;

    // W/ initializer
    car  : caractère  := 'A';
    intt : entier     := 5;
    real : réel       := 3.14;
    str  : chaîne(20) := "hello";

    i1, i2 : entier; // Declarator list
    c1, c2, c3 : caractère := 'C'; // Declarator list with initializers

    écrireEcran(car, intt, real, str);
    écrireEcran(file);
    écrireEcran(bol);
    écrireEcran(bol, file);
fin