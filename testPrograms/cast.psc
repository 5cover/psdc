programme Casts c'est
// A demonstration of implicit and explicit casts.

procédure print(entF s : chaîne); // to test for incomplete type

début
    b : booléen := vrai;
    c : caractère := 'A';
    i : entier := 5;
    r : réel := 3.14;
    ls : chaîne(20) := "Hello friend";

    
    print(entE ls);     // implicit: chaine(20) -> chaîne
    print(entE "abcd"); // implicit: chaine(4) -> chaîne
    r := i;             // implicit: entier -> réel
    r := 4;             // implicit: entier -> réel
    i := (entier)r;     // explicit: réel -> entier
    i := (entier)3.14;  // explicit: réel -> entier
    c := (caractère)i;  // explicit: entier -> caractère
    c := (caractère)65; // explicit: entier -> caractère
    i := (entier)c;     // explicit: caractère -> entier
    i := (entier)'A';   // explicit: caractère -> entier
    i := (entier)b;     // explicit: booléen -> entier
    i := (entier)vrai;  // explicit: booléen -> entier
    i := (entier)faux;  // explicit: booléen -> entier
    b := (booléen)5e;  // explicit: entier -> booléen
fin

procédure print(entF s : chaîne) c'est
début
    écrireEcran(s);
fin