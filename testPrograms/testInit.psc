programme TestInitializers c'est
// Showcases initializers of increasing complexity using all features.

// Simple initializer for a structure with a single integer member.
constante structure début
    x: entier;
fin a_simple := {
    1
}; #eval expr a_simple // x = 1

/*// Simple initializer for a structure with an integer and a character array.
constante structure début
    x: entier;
    c: tableau [4] de caractère;
fin a_simple2 := {
    2,
    { 'A', 'B', 'C', '\0' }
}; #eval expr a_simple2 // x = 2, c = { 'A', 'B', 'C', '\0' }

// Designated initializer for a structure with an integer and a character array.
constante structure début
    x: entier;
    c: tableau [4] de caractère;
fin a_designated := {
    .c := { 'D', 'E', 'F', '\0' },
    .x := 3
}; #eval expr a_designated // x = 3, c = { 'D', 'E', 'F', '\0' }

// Initializer for a structure with multiple integers.
constante structure début
    x, y, z: entier;
fin b_simple := {
    4,
    5,
    6
}; #eval expr b_simple // x = 4, y = 5, z = 6

// Designated initializer for a structure with multiple integers.
constante structure début
    x, y, z: entier;
fin b_designated := {
    .y := 7,
    .z := 8,
    .x := 9
}; #eval expr b_designated // x = 9, y = 7, z = 8

// Initializer for a nested structure.
type c_nested_t = structure début
    a: entier;
    b: structure début
        c: entier;
        d: tableau [2] de entier;
    fin;
fin

constante c_nested_t c_nested := {
    10,
    {
        11,
        { 12, 13 }
    }
}; #eval expr c_nested // a = 10, b.c = 11, b.d = { 12, 13 }

// Designated initializer for a nested structure.
constante c_nested_t c_nested_designated := {
    .a := 14,
    .b := {
        .d := { 15, 16 },
        .c := 17
    }
}; #eval expr c_nested_designated // a = 14, b.c = 17, b.d = { 15, 16 }

// Initializer for an array of structures.
type d_array_t = structure début
    x: entier;
    y: tableau [2] de entier;
fin

constante tableau [2] de d_array_t d_array := {
    {
        18,
        { 19, 20 }
    },
    {
        21,
        { 22, 23 }
    }
}; #eval expr d_array // d_array[0] = { 18, { 19, 20 } }, d_array[1] = { 21, { 22, 23 } }

// Designated initializer for an array of structures.
constante tableau [2] de d_array_t d_array_designated := {
    [0] := {
        .x := 24,
        .y := { 25, 26 }
    },
    [1] := {
        .x := 27,
        .y := { 28, 29 }
    }
}; #eval expr d_array_designated // d_array_designated[0] = { 24, { 25, 26 } }, d_array_designated[1] = { 27, { 28, 29 } }

// Initializer for a structure with nested arrays and structures.
type e_complex_t = structure début
    a: entier;
    b: structure début
        c: tableau [2] de entier;
        d: structure début
            e: entier;
            f: tableau [2] de entier;
        fin;
    fin;
fin

constante e_complex_t e_complex := {
    30,
    {
        { 31, 32 },
        {
            33,
            { 34, 35 }
        }
    }
}; #eval expr e_complex // a = 30, b.c = { 31, 32 }, b.d.e = 33, b.d.f = { 34, 35 }

// Designated initializer for a structure with nested arrays and structures.
constante e_complex_t e_complex_designated := {
    .a := 36,
    .b := {
        .c := { 37, 38 },
        .d := {
            .e := 39,
            .f := { 40, 41 }
        }
    }
}; #eval expr e_complex_designated // a = 36, b.c = { 37, 38 }, b.d.e = 39, b.d.f = { 40, 41 }

// Initializer for an array of nested structures.
type f_nested_array_t = structure début
    a: entier;
    b: structure début
        c: entier;
        d: tableau [2] de entier;
    fin;
fin

constante tableau [2] de f_nested_array_t f_nested_array := {
    {
        42,
        {
            43,
            { 44, 45 }
        }
    },
    {
        46,
        {
            47,
            { 48, 49 }
        }
    }
}; #eval expr f_nested_array // f_nested_array[0] = { 42, { 43, { 44, 45 } } }, f_nested_array[1] = { 46, { 47, { 48, 49 } } }

// Designated initializer for an array of nested structures.
constante tableau [2] de f_nested_array_t f_nested_array_designated := {
    [0] := {
        .a := 50,
        .b := {
            .c := 51,
            .d := { 52, 53 }
        }
    },
    [1] := {
        .a := 54,
        .b := {
            .c := 55,
            .d := { 56, 57 }
        }
    }
}; #eval expr f_nested_array_designated // f_nested_array_designated[0] = { 50, { 51, { 52, 53 } } }, f_nested_array_designated[1] = { 54, { 55, { 56, 57 } } }

// Initializer for a structure with nested arrays and structures, using designators and nested initializers.
type g_very_complex_t = structure début
    a: entier;
    b: structure début
        c: tableau [2] de entier;
        d: structure début
            e: entier;
            f: tableau [2] de entier;
        fin;
    fin;
fin

constante g_very_complex_t g_very_complex := {
    .a := 58,
    .b := {
        .c := { 59, 60 },
        .d := {
            .e := 61,
            .f := { 62, 63 }
        }
    }
}; #eval expr g_very_complex // a = 58, b.c = { 59, 60 }, b.d.e = 61, b.d.f = { 62, 63 }

// Initializer for an array of nested structures, using designators and nested initializers.
type h_very_nested_array_t = structure début
    a: entier;
    b: structure début
        c: entier;
        d: tableau [2] de entier;
    fin;
fin

constante tableau [2] de h_very_nested_array_t h_very_nested_array := {
    [0] := {
        .a := 64,
        .b := {
            .c := 65,
            .d := { 66, 67 }
        }
    },
    [1] := {
        .a := 68,
        .b := {
            .c := 69,
            .d := { 70, 71 }
        }
    }
}; #eval expr h_very_nested_array // h_very_nested_array[0] = { 64, { 65, { 66, 67 } } }, h_very_nested_array[1] = { 68, { 69, { 70, 71 } } }

// Initializer for a structure with nested arrays and structures, using designators and nested initializers, with trailing comma.
constante g_very_complex_t g_very_complex_trailing_comma := {
    .a := 72,
    .b := {
        .c := { 73, 74 },
        .d := {
            .e := 75,
            .f := { 76, 77 },
        },
    },
}; #eval expr g_very_complex_trailing_comma // a = 72, b.c = { 73, 74 }, b.d.e = 75, b.d.f = { 76, 77 }

// Empty initializer for a structure.
constante structure début
    n: entier;
fin i_empty := {
}; #eval expr i_empty // n = 0

// Empty initializer for an empty structure (should raise an error).
constante structure début
fin j_empty := {
}; #eval expr j_empty // Error: struct cannot be empty
/**/