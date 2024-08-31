programme TrailingCommas c'est
// Trailing commas are AWESOME!

// TRAILING COMMAS IN:

// Procedure formal parameter list
procédure greet(entF nom : chaîne, entF prenom : chaîne, ) c'est début
    // Bultin parameters
    écrireEcran("Bonjour, ", prenom, " ", nom, "!", );
fin

// Function formal parameter list
fonction twice(entF x : entier, ) délivre entier c'est début
    retourne x * 2;
fin

début
    // Procedure actual paremeter list
    greet(entE "NoLastName", entE "Scover", );

    // Function actual parameter list
    écrireEcran(twice(entE 5, ), );

    // Local variable list
    a, b, c, : tableau[5,5,] de entier;

    // Array subscript
    écrireEcran(a[1,1,]);

    // Braced initializers
    d : tableau[3, ] de entier := { 4,2,[3,]:=0, };
fin