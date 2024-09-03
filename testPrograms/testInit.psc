programme TestInitializers c'est
// Showcases initializers of increasing complexity using all features.

// Simple initializer for a structure with a single integer member.
constante structure début
    x: entier;
fin a_simple := {
    1
};
#assert a_simple.x == 1

// Simple array initializer
constante tableau [4] de caractère a_simple_array := { 'A', 'B', 'C', '\0' };
#assert a_simple_array[1] == 'A'
#assert a_simple_array[2] == 'B'
#assert a_simple_array[3] == 'C'
#assert a_simple_array[4] == '\0'

// Simple initializer for a structure with an integer and a character array.
constante structure début
    x: entier;
    c: tableau [4] de caractère;
fin a_simple2 := {
    2,
    { 'A', 'B', 'C', '\0' }
};
#assert a_simple2.x == 2
#assert a_simple2.c[1] == 'A'
#assert a_simple2.c[2] == 'B'
#assert a_simple2.c[3] == 'C'
#assert a_simple2.c[4] == '\0'

// Designated initializer for a structure with an integer and a character array.
constante structure début
    x: entier;
    c: tableau [4] de caractère;
fin a_designated := {
    .c := { 'D', 'E', 'F', '\0' },
    .x := 3
};
#assert a_designated.x == 3;
#assert a_designated.c[1] == 'D'
#assert a_designated.c[2] == 'E'
#assert a_designated.c[3] == 'F'
#assert a_designated.c[4] == '\0'

// Initializer for a structure with multiple integers.
constante structure début
    x, y, z: entier;
fin b_simple := {
    4,
    5,
    6
};
#assert b_simple.x == 4
#assert b_simple.y == 5
#assert b_simple.z == 6

// Designated initializer for a structure with multiple integers.
constante structure début
    x, y, z: entier;
fin b_designated := {
    .y := 7,
    .z := 8,
    .x := 9
};
#assert b_designated.x == 9
#assert b_designated.y == 7
#assert b_designated.z == 8

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
};
#assert c_nested.a == 10
#assert c_nested.b.c == 11
#assert c_nested.b.d[1] == 12
#assert c_nested.b.d[2] == 13

// Designated initializer for a nested structure.
constante c_nested_t c_nested_designated := {
    .a := 14,
    .b := {
        .d := { 15, 16 },
        .c := 17
    }
};
#assert c_nested_designated.a == 14
#assert c_nested_designated.b.c == 17
#assert c_nested_designated.b.d[1] == 15
#assert c_nested_designated.b.d[2] == 16

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
};
#assert d_array[1].x == 18
#assert d_array[1].y[1] == 19
#assert d_array[1].y[2] == 20
#assert d_array[2].x == 21
#assert d_array[2].y[1] == 22
#assert d_array[2].y[2] == 23

// Designated initializer for an array of structures.
constante tableau [2] de d_array_t d_array_designated := {
    [2] := {
        .x := 27,
        .y := { 28, 29 }
    },
    [1] := {
        .x := 24,
        .y := { 25, 26 }
    }
};
#assert d_array_designated[1].x == 24
#assert d_array_designated[1].y[1] == 25
#assert d_array_designated[1].y[2] == 26
#assert d_array_designated[2].x == 27
#assert d_array_designated[2].y[1] == 28
#assert d_array_designated[2].y[2] == 29

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
};
#assert e_complex.a == 30
#assert e_complex.b.c[1] == 31
#assert e_complex.b.c[2] == 32
#assert e_complex.b.d.e == 33
#assert e_complex.b.d.f[1] == 34
#assert e_complex.b.d.f[2] == 35

// Designated initializer for a structure with nested arrays and structures.
constante e_complex_t e_complex_designated := {
    .b := {
        .c := { 37, 38 },
        .d := {
            .e := 39,
            .f := { [2] := 41, [1] := 40 }
        }
    },
    .a := 36
};
#assert e_complex_designated.a == 36
#assert e_complex_designated.b.c[1] == 37
#assert e_complex_designated.b.c[2] == 38
#assert e_complex_designated.b.d.e == 39
#assert e_complex_designated.b.d.f[1] == 40
#assert e_complex_designated.b.d.f[2] == 41

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
};
#assert f_nested_array[1].a == 42
#assert f_nested_array[1].b.c == 43
#assert f_nested_array[1].b.d[1] == 44
#assert f_nested_array[1].b.d[2] == 45
#assert f_nested_array[2].a == 46
#assert f_nested_array[2].b.c == 47
#assert f_nested_array[2].b.d[1] == 48
#assert f_nested_array[2].b.d[2] == 49

// Designated initializer for an array of nested structures.
constante tableau [2] de f_nested_array_t f_nested_array_designated := {
    [1] := {
        .a := 50,
        .b := {
            .c := 51,
            .d := { 52, 53 }
        }
    },
    [2] := {
        .a := 54,
        .b := {
            .c := 55,
            .d := { 56, 57 }
        }
    }
};
#assert f_nested_array_designated[1].a == 50
#assert f_nested_array_designated[1].b.c == 51
#assert f_nested_array_designated[1].b.d[1] == 52
#assert f_nested_array_designated[1].b.d[2] == 53
#assert f_nested_array_designated[2].a == 54
#assert f_nested_array_designated[2].b.c == 55
#assert f_nested_array_designated[2].b.d[1] == 56
#assert f_nested_array_designated[2].b.d[2] == 57

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
};
#assert g_very_complex.a == 58
#assert g_very_complex.b.c[1] == 59
#assert g_very_complex.b.c[2] == 60
#assert g_very_complex.b.d.e == 61
#assert g_very_complex.b.d.f[1] == 62
#assert g_very_complex.b.d.f[2] == 63

// Initializer for an array of nested structures, using designators and nested initializers.
type h_very_nested_array_t = structure début
    a: entier;
    b: structure début
        c: entier;
        d: tableau [2] de entier;
    fin;
fin

constante tableau [2] de h_very_nested_array_t h_very_nested_array := {
    [1] := {
        .a := 64,
        .b := {
            .c := 65,
            .d := { 66, 67 }
        }
    },
    [2] := {
        .a := 68,
        .b := {
            .c := 69,
            .d := { 70, 71 }
        }
    }
};
#assert h_very_nested_array[1].a == 64
#assert h_very_nested_array[1].b.c == 65
#assert h_very_nested_array[1].b.d[1] == 66
#assert h_very_nested_array[1].b.d[2] == 67
#assert h_very_nested_array[2].a == 68
#assert h_very_nested_array[2].b.c == 69
#assert h_very_nested_array[2].b.d[1] == 70
#assert h_very_nested_array[2].b.d[2] == 71

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
};
#assert g_very_complex_trailing_comma.a == 72
#assert g_very_complex_trailing_comma.b.c[1] == 73
#assert g_very_complex_trailing_comma.b.c[2] == 74
#assert g_very_complex_trailing_comma.b.d.e == 75
#assert g_very_complex_trailing_comma.b.d.f[1] == 76
#assert g_very_complex_trailing_comma.b.d.f[2] == 77
#eval expr g_very_complex_trailing_comma

// Empty initializer for a structure.
constante structure début
    n: entier;
fin i_empty := { };
#assert i_empty.n == 0;

// Empty initializer for an empty structure.
constante structure début
fin j_empty := {
};
