programme ComptimeTest c'est
// Tests proper evaluation of comptime expressions

// Literals
#assert (1   ) == 1
#assert ('a' ) == 'a'
#assert ("a" ) == "a"
#assert (3.14) == 3.14
#assert (vrai) == vrai
#assert (faux) == faux

// Operations

/// Addition
#assert (1 + 2     ) == 3
#assert (1.5 + 2   ) == 3.5
#assert (1.5 + 2.5 ) == 4

/// Subscration
#assert (1 - 2     ) == -1
#assert (1.5 - 2   ) == -.5
#assert (1.5 - 2.5 ) == -1

/// Multiplication
#assert (1 * 2     ) == 2
#assert (1.5 * 2   ) == 3
#assert (1.5 * 2.5 ) == 3.75

/// Division
#assert (1 / 2     ) == 0
#assert (1.5 / 2   ) == .75
#assert (1.5 / 2.5 ) == .6

/// Modulo
#assert (1 % 2     ) == 1
#assert (1.5 % 2   ) == 1.5
#assert (1.5 % 2.5 ) == 1.5

/// Equality
#assert (1 == 2    )   == faux
#assert (1.5 == 2  )   == faux
#assert (1.5 == 2.5)   == faux
#assert ("AB" == "BA") == faux

#assert (1 == 1    )   == vrai
#assert (1.0 == 1  )   == vrai
#assert (1.5 == 1.5)   == vrai
#assert ("AB" == "AB") == vrai

/// Inequality
#assert (1 != 2    )   == vrai
#assert (1.5 != 2  )   == vrai
#assert (1.5 != 2.5)   == vrai
#assert ("AB" != "BA") == vrai

#assert (1 != 1    )   == faux
#assert (1.0 != 1  )   == faux
#assert (1.5 != 1.5)   == faux
#assert ("AB" != "AB") == faux

/// Less than

//// l < r
#assert (1 < 2     ) == vrai
#assert (1.5 < 2   ) == vrai
#assert (1.5 < 2.5 ) == vrai

//// l > r
#assert (2 < 1     ) == faux
#assert (2 < 1.5   ) == faux
#assert (2.5 < 1.5 ) == faux

//// l = r
#assert (1 < 1     ) == faux
#assert (1.0 < 1   ) == faux
#assert (1.5 < 1.5 ) == faux

/// Less than or equal

//// l < r
#assert (1 <= 2    ) == vrai
#assert (1.5 <= 2  ) == vrai
#assert (1.5 <= 2.5) == vrai

//// l > r
#assert (2 <= 1    ) == faux
#assert (2 <= 1.5  ) == faux
#assert (2.5 <= 1.5) == faux

//// l = r
#assert (1 <= 1    ) == vrai
#assert (1.0 <= 1  ) == vrai
#assert (1.5 <= 1.5) == vrai

/// Greater than

//// l < r
#assert (1 > 2    ) == faux
#assert (1.5 > 2  ) == faux
#assert (1.5 > 2.5) == faux

//// l > r
#assert (2 > 1    ) == vrai
#assert (2 > 1.5  ) == vrai
#assert (2.5 > 1.5) == vrai

//// l = r
#assert (1 > 1    ) == faux
#assert (1.0 > 1  ) == faux
#assert (1.5 > 1.5) == faux

/// Greater than or equal

//// l < r
#assert (1 >= 2    ) == faux
#assert (1.5 >= 2  ) == faux
#assert (1.5 >= 2.5) == faux

//// l > r
#assert (2 >= 1    ) == vrai
#assert (2 >= 1.5  ) == vrai
#assert (2.5 >= 1.5) == vrai

//// l = r
#assert (1 >= 1    ) == vrai
#assert (1.0 >= 1  ) == vrai
#assert (1.5 >= 1.5) == vrai

/// Logical conjunction
#assert (faux et faux) == faux
#assert (faux et vrai) == faux
#assert (vrai et faux) == faux
#assert (vrai et vrai) == vrai

/// Logical disjunction
#assert (faux ou faux) == faux
#assert (faux ou vrai) == vrai
#assert (vrai ou faux) == vrai
#assert (vrai ou vrai) == vrai

/// Logical negation
#assert (non faux) == vrai
#assert (non vrai) == faux

/// Logical equivalence
#assert (faux == faux) == vrai
#assert (faux == vrai) == faux
#assert (vrai == faux) == faux
#assert (vrai == vrai) == vrai

/// Logical exclusive or
#assert (faux xor faux) == faux
#assert (faux xor vrai) == vrai
#assert (vrai xor faux) == vrai
#assert (vrai xor vrai) == faux

/// Casts

//// From real to integer
#assert ((entier)1.5) == 1
#assert ((entier)2.5) == 2

//// From bool to integer
#assert ((entier)faux) == 0
#assert ((entier)vrai) == 1

//// From character to integer (runtime-known)
#eval expr ((entier)'a')
#eval expr ((entier)'A')

//// From integer to character (runtime-known)
#eval expr ((caractère)97)
#eval expr ((caractère)65)

//// Casts using implicit conversions
#assert ((réel)1) == 1.0
#assert ((entier)1.5) == 1
#assert ((chaîne)"Hello") == "Hello"

// Constants

/// Scalar constant
constante entier C_INT := 1 - 8 + 8 * 1;
#eval expr C_INT

// Array constant
type tArrInt = tableau[3] de entier;
constante tArrInt C_ARR_INT := { 1, 2, 3 };
constante tArrInt C_ARR_INT_SOME_DES := { 1, [2] := 2, 3 };
constante tArrInt C_ARR_INT_ONLY_DES := { [3] := 3, [2] := 2, [1] := 1 };
#eval expr C_ARR_INT
#eval expr C_ARR_INT_SOME_DES
#eval expr C_ARR_INT_ONLY_DES

// Structure constant
type tPoint = structure début x, y : réel; fin;
constante tPoint C_POINT := { 3.14, 14.3 };
constante tPoint C_POINT_SOME_DES := { .x := 3.14, 14.3 };
constante tPoint C_POINT_ONLY_DES := { .x := 3.14, .y := 14.3 };
#eval expr C_POINT
#eval expr C_POINT_SOME_DES
#eval expr C_POINT_ONLY_DES