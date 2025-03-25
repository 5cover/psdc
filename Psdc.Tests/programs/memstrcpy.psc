programme MemStrCpy c'est
// Test array and string assignment

constante chaîne(20) S := "chaîne";
type tTab = tableau[3] de entier;
constante tTab V := {1,2,3};

début
    v, w : tTab;
    v := V;
    w := v;

    s, t : chaîne(20);

    s := S;
    t := s;
fin