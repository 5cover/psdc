constante entier MAX := 1000;
type t_matrice = tableau[MAX, MAX] de entier;

/*
2. a. Cette matrice est carrée
la valeurà [l, c] donne le PGCD(l, c)
*/

/*
3. Écrire la procédure initialiser() qui permet initialiser une matrice de type t_matrice
*/
procédure initialiser(sortF m : t_matrice) c'est début
    pour l de 1 à MAX faire
        pour c de 1 à MAX faire
            m[l, c] := 0;
        fin pour
    fin pour

    pour l de 1 à MAX faire
        pour c de 1 à MAX faire
            si m[l, c] == 0 alors
                si l == c alors
                    m[l, c] := l;
                sinon si l > c
                    m[l, c] := m[l - c, c];
                    m[c, l] := m[l, c];
                finsi
            finsi
        fin pour
    fin pour
fin

/*
4. On définit le PGCD de trois nombres PGCD(n1,n2,n3) comme étant le PGCD du PGCD(n1,n2) et de n3. En utilisant cette propriété écrire la fonction retournant le PGCD d'une série de nombres qui auront été préalablement saisis dans un tableau
*/
fonction PGCD(entF n : tableau[MAX] de entier, entF nb : entier) : délivre entier c'est début
    pgcd : entier;
    pgcd := n[1];
    pour i de 1 à nb faire
        pgcd := matricePgcd(pgcd, n[i]);
    finfaire
    retourne pgcd;
fin
