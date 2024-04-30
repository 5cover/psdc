programme Variables c'est
// Showcasing different variable types and their formatting

début
    bol : booléen;
    car : caractère;
    intt : entier;
    real : réel;
    str : chaîne(20);

    file : nomFichierLog;

    car := 'a';
    intt := 5;
    real := 3.14;
    str := "hello";
    bol := faux;
    bol := vrai;

    écrireEcran(car, intt, real, str);
    écrireEcran(file);
    écrireEcran(bol);
    écrireEcran(bol, file);
fin