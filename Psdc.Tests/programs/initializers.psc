programme Initializers c'est
// Showcases the usage of designated and undesignated initializers for array and structure types.

// Examples based on https://en.cppreference.com/w/c/language/struct_initialization

constante structure début
    x: entier;
    c: tableau [4] de caractère;
fin a_u  := {
    1
};
#assert a_u.x == 1
#assert a_u.c[1] == '\0'
#assert a_u.c[2] == '\0'
#assert a_u.c[3] == '\0'
#assert a_u.c[4] == '\0'

constante structure début
    x: entier;
    c: tableau [4] de caractère;
fin a_u2 := {
    .c := {
        'A'
    }
};
#assert a_u2.x == 0
#assert a_u2.c[1] == 'A'
#assert a_u2.c[2] == '\0'
#assert a_u2.c[3] == '\0'
#assert a_u2.c[4] == '\0'

// When initializing a struct, the first initializer in the list initializes the first declared member (unless a designator is specified), and all subsequent initializers without designators initialize the struct members declared after the one initialized by the previous expression.

constante structure début
    x,y,z : réel;
fin b_p := {
    1.2,
    1.3
};
#assert b_p.x == 1.2;
#assert b_p.y == 1.3;
#assert b_p.z == 0;

type b_div_t = structure
    début quot,rem: entier;
fin;
constante b_div_t b_answer := {
    .quot := 2,
    .rem := -1
};
#assert b_answer.quot == 2
#assert b_answer.rem == -1

// A designator causes the following initializer to initialize the struct member described by the designator. Initialization then continues forward in order of declaration, beginning with the next element declared after the one described by the designator.

constante structure début
    sec,min,hour,day,mon,year: entier;
fin c_z := {
    .day:=31,
    12,
    2014,
    .sec:=30,
    15,
    17
};
#assert c_z.sec == 30
#assert c_z.min == 15
#assert c_z.hour == 17
#assert c_z.day == 31
#assert c_z.mon == 12
#assert c_z.year == 2014

// It's an error to provide more initializers than members.

// If the members of the struct or union are arrays, structs, or unions, the corresponding initializers in the brace-enclosed list of initializers are any initializers that are valid for those members, except that their braces may be omitted as follows:

// If the nested initializer begins with an opening brace, the entire nested initializer up to its closing brace initializes the corresponding member object. Each left opening brace establishes a new current object. The members of the current object are initialized in their natural order, unless designators are used: array elements in subscript order, struct members in declaration order, only the first declared member of any union. The subobjects within the current object that are not explicitly initialized by the closing brace are empty-initialized. 

// NOT YET IMPLEMENTED

/*type d_example = structure début
    addr : structure début
        port : entier;
    fin;
    in_u : structure début
        a8 : tableau[4] de entier;
        a16 : tableau[2] de entier;
    fin;
fin;

constante d_example d_ex := { // start of initializer list for struct example
    { // start of initializer list for ex.addr
        80 // initialized struct's only member
    }, // end of initializer list for ex.addr
    { // start of initializer-list for ex.in_u
        { // initializes first element of the structure
            127,
            0,
            0,
            1
        } 
    }
};
#assert d_ex.addr.port == 80
#assert d_ex.in_u.a8[1] == 127
#assert d_ex.in_u.a8[2] == 0
#assert d_ex.in_u.a8[3] == 0
#assert d_ex.in_u.a8[4] == 1
#assert d_ex.in_u.a16[1] == 0
#assert d_ex.in_u.a16[2] == 0

// If the nested initializer does not begin with an opening brace, only enough initializers from the list are taken to account for the elements or members of the member array, struct or union; any remaining initializers are left to initialize the next struct member: 

// In other words: undesignated scalar initializers mutate the corresponding value in the deepest sub-object.

constante d_example e_ex := {
    80, // 80 initializes ex.addr.port
    127, // 127 initializes ex.in_u.a8[0]
    0, // 0 initializes ex.in_u.a8[1]
    0, // 0 initializes ex.in_u.a8[2]
    1 // 1 initializes ex.in_u.a8[3]
};

#assert e_ex.addr.port == 80
#assert e_ex.in_u.a8[1] == 127
#assert e_ex.in_u.a8[2] == 0
#assert e_ex.in_u.a8[3] == 0
#assert e_ex.in_u.a8[4] == 1
#assert e_ex.in_u.a16[1] == 0
#assert e_ex.in_u.a16[2] == 0

// When designators are nested, the designators for the members follow the designators for the enclosing structs/unions/arrays. Within any nested bracketed initializer list, the outermost designator refers to the current object and selects the subobject to be initialized within the current object only. 

constante d_example f_ex2 := {
    // current object is ex2, designators are for members of example
    .in_u.a8[0]:=127,
    0,
    0,
    1,
    .addr:=80
}; #eval expr f_ex2

constante d_example f_ex3 := {
    80,
    .in_u:={ // changes current object to ex.in_u
        127,
        .a8[2]:=1 // this designator refers to of in_u
    }
}; #eval expr f_ex3

// If any subobject is explicitly initialized twice (which may happen when designators are used), the initializer that appears later in the list is the one used (the earlier initializer may not be evaluated):

fonction g_printf(entF str : chaîne) délivre entier c'est début
    écrireEcran(str);
    retourne 10; // whatever
fin

// Raises a warning for double initialization with side effects
constante structure début
    n : entier;
fin g_s := {
    g_printf(entE "a\n"),
    .n:=g_printf(entE "b\n")
}; #eval expr g_s

// Although any non-initialized subobjects are initialized implicitly, implicit initialization of a subobject never overrides explicit initialization of the same subobject if it appeared earlier in the initializer list:

type h_T = structure début
    k, l : entier;
    a : tableau[2] de entier;
fin;

type h_S = structure début
    i : entier;
    t : T;
fin;

constante h_T h_x := {
    .l := 43,
    .k := 42,
    .a[1] := 19,
    .a[0] := 18
}; #eval expr h_x   // h_x initialized to {42, 43, {18, 19} }

constante h_S h_l := {
    1,              // initializes h_l.i to 1
    .t := h_x,       // initializes h_l.t to {42, 43, {18, 19} }
    .t.l := 41,      // changes h_l.t to {42, 41, {18, 19} }
    .t.a[1] := 17    // changes h_l.t to {42, 41, {18, 17} }
}; #assert h_l.t.k == 42;
// .t = h_x sets h_l.t.k to 42 explicitly
// .t.l = 41 would zero out h_l.t.k implicitly

// However, when an initializer begins with a left open brace, its current object is fully re-initialized and any prior explicit initializers for any of its subobjects are ignored:

type i_fred = structure début
    s: tableau[4] de caractère;
    n: entier;
fin;

constante tableau[1]
de i_fred i_x := {
    {
        { "abc" },
        1
    },                // inits i_x[0] to { {'a','b','c','\0'}, 1 }
    [0].s[0] := 'q'   // changes i_x[0] to { {'q','b','c','\0'}, 1 }
}; #eval expr i_x

constante tableau[1]
de i_fred i_y := {
    {
        { "abc" },
        1
    },                // inits i_y[0] to { {'a','b','c','\0'}, 1 }
    [0] := {          // current object is now the entire y[0] object
        .s[0] := 'q' 
    }                 // replaces i_y[0] with { {'q','\0','\0','\0'}, 0 }
}; #eval expr i_y

// The initializer list may have a trailing comma, which is ignored. 

//constante structure début x,y : réel; fin j_p := {
//    1.0,
//    2.0, // trailing comma OK
//}; #eval expr j_p

// The initializer list can be empty.

constante structure début
    n : entier;
fin k_s := {
    0
}; #eval expr k_s // OK

constante structure début
    n : entier;
fin l_s := {
}; #eval expr l_s // OK: s.n is initialized to 0

constante structure début
fin m_s := {
}; #eval expr m_s // Error: struct cannot be empty
/**/