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

structure début sec,min,hour,day,mon,year:entier; fin z
    := {.day:=31,12,2014,.sec:=30,15,17}; // initializes z to {30,15,17,31,12,2014}

