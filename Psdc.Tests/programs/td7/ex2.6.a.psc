/*
Ex. 2.6.a TD7 Dév

Écrire en langage algorithmique la fonction booléenne existe() qui, à partir d'une valeur n, du 
tableau tab et de son nombre d'éléments tous trois donnés en paramètre, délivre vrai si n est présente dans tab 
et faux sinon.
*/

programme tableaux c'est
début

fonction existe(entF tab : tableau[] de entier, entF nbElements : entier, entF valeurRecherchee : entier) délivre booléen c'est
début
    trouve : booléen;
    i : entier;

    trouve := faux;
    i := 1;

    tant que non(trouve) et (i <= nbElements) faire
        trouve := tab[i] == valeurRecherchee;
        i := i + 1;
    finfaire

    retourne trouve;
fin