programme ex1 c'est

constante entier MAXNUM := 3;
constante entier MAXPERS := 10;

type t_tabNumeros = tableau[MAXNUM] de entier;

type t_personne = structure début
    c_nom : chaîne(30);
    c_liste : t_tabNumeros;
    c_nbComptes : entier;
fin;

type t_tabPersonnes = tableau[MAXPERS] de t_personne;

/*
1. Écrire la procédure permuter() qui permute les contenus respectifs de p1 et p2 de type t_personne avec un minimum d'instructions
*/
procédure permuter(entF/sortF p1 : t_personne, entF/sortF p2 : t_personne) c'est début
    tmp : t_personne;
    tmp := p1;
    p1 := p2;
    p2 := tmp;
fin

/*
2. Écrire la procédure afficherPersonne() qui affiche à l'écran le nom de la personne p et la liste des ses numéros de compte bancaires
*/
procédure afficherPersonne(entF p : t_personne) c'est début
    écrireEcran(p.c_nom);
    écrireEcran("Comptes : ", p.c_nbComptes);
    i : entier;
    pour (i de 1 à p.c_nbComptes) faire
        écrireEcran(i, " : ", p.c_liste[i]);
    finfaire
fin

/*
3. Écrire une procédure triNaif() qui trie le tableau t sur le nom (par ordre croissant) suivant la méthode dite du « tri naïf » :
*/
procédure triNaif(entF/sortF t : t_tabPersonnes, entF nbPers : entier) c'est début
    iMin : entier;

    i : entier;
    pour (i de 1 à nbPers) faire
        iMin := i;
        
        iRecMin : entier;
        pour (iRecMin de i + 1 à nbPers) faire
            si (t[iRecMin].c_nom < t[iMin].c_nom) alors
                iMin := iRecMin;
            finsi
        finfaire

        permuter(entE/sortE t[i], entE/sortE t[iMin]);
    finfaire
fin

/*
4. Écrire une procédure afficherTableau() qui affiche à l'écran les nbPers premiers éléments de t.
*/
procédure afficherTableau(entF t : t_tabPersonnes, entF nbPers : entier) c'est début
    i : entier;
    pour (i de 1 à nbPers) faire
        afficherPersonne(entE t[i]);
    finfaire
fin

/*
5. Programme principal
*/
début
    BANQUE : t_tabPersonnes;
    p : t_personne;
    p.c_nom := "toto";
    p.c_nbComptes := 3;
    p.c_liste[1] := 21;
    p.c_liste[2] := 25;
    p.c_liste[3] := 12;
    BANQUE[1] := p;
    p.c_nom := "dupont";
    p.c_nbComptes := 1;
    p.c_liste[1] := 56;
    BANQUE[2] := p;
    p.c_nom := "albert";
    p.c_nbComptes := 3;
    p.c_liste[1] := 19;
    p.c_liste[2] := 123;
    p.c_liste[3] := 111;
    BANQUE[3] := p;
    p.c_nom := "alfred";
    p.c_nbComptes := 2;
    p.c_liste[1] := 20;
    p.c_liste[2] := 321;
    BANQUE[4] := p;

    banque : t_tabPersonnes := {
        {"toto", {21,25,12}, 3},
        {"dupont", {56}, 1},
        {"albert", {19,123,111}, 3},
        {"alfred", {20,312}, 2}
    };

    i,nb : entier;
    nb := 4;
    pour i de 1 à nb faire
        banque[i] := BANQUE[i];
    finfaire //finPour

    afficherTableau(entE banque, entE nb);
    triNaif(entE/sortE banque, entE nb);
    afficherTableau(entE banque, entE nb);
fin