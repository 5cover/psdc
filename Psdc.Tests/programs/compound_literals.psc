programme CompoundLiterals c'est
// Tests the proper handling of compound literals and initializers.
// A variable initializer shouldn't use a compound literal as this requires a copy (and causes a compiler error with arrays)
// But we need componds literals for every other appearance of the constant name.

type T = tableau[3,3] de entier; // non literal-able type -> values must be expressed as compound literals in non-initializer contexts.
type U = chaîne(20); // literal-able type -> can be used without restrictions

constante T C_T
    := {{ 7, 8, 9 },
        { 4, 5, 6 },
        { 1, 2, 3 }};

constante U C_U := "abc";

procédure accept_T(entF t : T) c'est début
    écrireEcran(t[2,2]);
fin

procédure accept_U(entF u : U) c'est début
    écrireEcran(u);
fin

début
    a : T := C_T; // compound literal here

    accept_T(entE C_T); // no compound literal here
    accept_U(entE C_U); // compound literal here
fin