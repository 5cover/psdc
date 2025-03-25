/*
Ex 5 TD9 dév.
*/

constante entier N := 10;
constante entier BOMBE := 9; // indicateur de bombe sur le plateau joué

/*
1. Proposez une définition d'un type plateau de démineur en tant que tableau
*/
type t_plateau := tableau[N][N] de entier;

/*
2. Proposez une procédure qui prend en pramètre le tableau joue (encore non initialisé) qui fait appel à la procédure positionMines pour avoir les positions des mines à placer et qui initialise le plateau joué en y insérant le chiffre 9 à chaque emplacement de mine.
*/
procédure placerMines(sortF plateauJouer : t_plateau) c'est
début
    i : entier;
    mines : tableau[N] de entier;
    positionMines(sortE mines);

    pour i de 1 à N faire
        plateauJouer[mines[i] / N][mines[i] % N] := 9;
    finfaire
fin

/*
3. Proposez une fonction qui prend en paramètre le tableau joué, un indice de ligne et un indice de colonne d'une case de ce plateau, et qui délivre le nombre de mines adjacentes à la case. Attention à ne pas explorer l'extérieur du panneau.
*/
fonction minesAdjacentes(entF plateauJoue : t_plateau, entF ligne : entier, entF colonne : entier) délivre entier c'est
début
    nbBombesVoisines, l, c, cDeb, cFin, lDeb, lFin : entier;
    nbBombesVoisines := 0 : entier;

    cDeb := colonne - 1;
    si colonne == 1 alors
        cDeb := 1;
    sinon

    cFin := colonne + 1
    si colonne == N alors
        cFin := N;
    finsi

    lDeb := ligne - 1;
    si ligne == 1 alors
        lDeb := 1:
    finsi

    lFin = ligne + 1;
    si ligne == N alors
        lFin := N;
    finsi

    pour l de lDeb à lFin faire
        pour c de cDeb à cFin faire
            si plateauJoue[lDeb][lFin] == BOMBE alors
                nbBombesVoisines := nbBombesVoisines + 1;
            finsi
    finfaire

    // Dans le cas où il y a une bombe à l'emplacement passé à la fonction, on diminue de 1, car ne souhaite pas compter les bombes sur la case.
    si plateauJoue[ligne][colonne] == BOMBE alors
        nbBombesVoisines := nbBombesVoisines - 1;
    finsi

    retourne nbBombesVoisines;
fin

/*
4. Proposez une procédure remplirPlateau prenant en paramètre d'E/S un plateau joué, calculant pour chaque case ne possédant pas une mine, le nombre de mines adjacentes et inscrivant ce nombre dans la case.
*/
procédure remplirPlateau(entF/sortF plateauJoue : t_plateau) c'est
début
    l, c : entier;
    pour l de 1 à N faire
        pour c de 1 à N faire
            plateauJoue[l][c] := minesAdjacentes(entE plateauJoue, entE l, entE c);
        finfaire
    finfaire
fin

/*
5. On s'intéresse mainteant au plateau observé par le joueur. Proposer une procédure permettant d'initialiser chaque case de ce tableau avec le valeur -1.
*/
procédure initPlateauObs(sortF plateauObs : t_plateau) c'est
début
    l, c : entier;
    pour l de 1 à N faire
        pour c de 1 à N faire
            plateauJoue[l][c] := -1;
        finfaire
    finfaire
fin

/*
6. Proposez une procédure jouer qui prend en paramètre les deux plateaux, i.e. celui des emplacements des mines et des balises ansi que le plateau observé par l'utilisateur
Cette procédure demande à l'utilisateur une coordonnée (indice de ligne et de colonne) et vérifie qu'il s'agit bien d'une case encore cachée.
Si l'utilisateur tombe sur une mine, il perd une vie sur les N/2 qu'il possède au départ.
Le jeu s'arrête si le joueur n'a plus de vie ou s'il a toruvé tous les emplacements sans mine.
*/
procédure jouer(entF plateauJoue : t_plateau, entF plateauObs : t_plateau) c'est
début
    
fin