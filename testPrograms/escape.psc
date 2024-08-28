programme EscapeUnescape c'est
// Test unescaping/escaping of string literals

constante chaîne S := "bonjour\r\nje m'apélle \"ludovick\"\tj'ai \u2467 ans\0. Ding \7! Dong \a!";

#eval expr S
//#assert S == "bonjour\r\nje m'apélle ludovick\tj'ai \u2467 ans\0. Ding \7! Dong \a!";
#assert S == S

début
    écrireEcran(S);
fin