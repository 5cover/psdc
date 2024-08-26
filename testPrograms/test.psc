programme Initializers c'est
// Showcases the usage of designated and undesignated initializers for array and structure types.

// Examples based on https://en.cppreference.com/w/c/language/struct_initialization

type T1 = structure début x: entier; c: tableau [4] de caractère; fin;
constante T1 u  := { 1 }            ; #eval expr u  // x = 1, c = garbage
constante T1 u2 := { .c := { 'A' } }; #eval expr u2 // x = 0, c = { 'A', '\0', '\0', '\0' }

type point = structure début x, y, z : réel; fin;
constante point p := { 1.2, 1.3 }; #eval expr p // p.x=1.2, p.y=1.3, p.z=0.0;

type div_t = structure début quot, rem : entier; fin;
constante div_t answer := { .quot := 2, .rem := -1 }; #eval expr answer

type example = structure début
    addr : structure début
        port : entier;
    fin;
    in_u : structure début
        a8 : tableau[4] de entier;
        a16 : tableau[2] de entier;
    fin;
fin;
constante example ex := { // start of initializer list for struct example
                         { // start of initializer list for ex.addr
                             80 // initialized struct's only member
                         }, // end of initializer list for ex.addr
                         { // start of initializer-list for ex.in_u
                             {127,0,0,1} // initializes first element of the union
                         } }; #eval expr ex
constante example ex1 := {80, 127, 0, 0, 1}; // 80 initializes ex.addr.port
                                             // 127 initializes ex.in_u.a8[0]
                                             // 0 initializes ex.in_u.a8[1]
                                             // 0 initializes ex.in_u.a8[2]
                                             // 1 initializes ex.in_u.a8[3]
#eval expr ex1
constante example ex2 := { // current object is ex2, designators are for members of example
                           .in_u.a8[0]:=127, 0, 0, 1, .addr:=80}; #eval expr ex2
constante example ex3 := {80, .in_u:={ // changes current object to ex.in_u
                              127,
                              .a8[2]:=1 // this designator refers to of in_u
                          } }; #eval expr ex3

procédure printf(entF str : chaîne) c'est début
    écrireEcran(str);
fin

// Raises a warning for double initialization with side effects
structure début entier n; fin s := {printf(entE "a\n"),
                                   .n:=printf(entE "b\n")};

