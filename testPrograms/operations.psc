programme Operations c'est
// Oerations and implicit conversions example

procédure proc(entF s1 : chaîne(20), entF s2 : chaîne);

début
    b : booléen; // boolean
    c : caractère; // character
    i : entier; // integer
    r : réel; // real
    s : chaîne(20); // 20-char long string

    // boolean operations
    b := b == b;
    b := b != b;
    b := b ET b;
    b := b OU b;
    b := b XOR b;

    b := NON b;

    // character operations
    b := c == c;
    b := c != c;

    // string operations
    b := s == s;
    b := s != s;

    // integer operations
    b := i == i;
    b := i != i;
    i := i / i;
    i := i - i;
    i := i + i;
    i := i * i;
    i := i % i;

    i := +i;
    i := -i;

    // real operations
    b := r == r;
    b := r != r;
    r := r / r;
    r := r - r;
    r := r + r;
    r := r * r;
    r := r % r;

    r := +r;
    r := -r;

    // implicit conversions
    r := i + r;
    proc(entE s, entE s);
fin

procédure proc(entF s1 : chaîne(20), entF s2 : chaîne) c'est début
    b : booléen;
    b := s1 == s2;
    b := s1 != s2;
fin