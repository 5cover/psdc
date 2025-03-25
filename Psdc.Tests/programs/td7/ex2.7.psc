/*
Ex. 2.7 TD7 Dév

Écrire la fonction fDouble() indiquant si le tableau tab transmis en paramètre contient au moins deux 
fois la même valeur.
*/

programme tableau c'est

fonction fDouble(entF tab : tableau[] de entier, entF nbElements : entier) délivre booléen c'est
début
    i : entier;
    j : entier;
    valeur : entier;
    valeurDupliquee : booléen;
    valeurDupliquee := faux;
    i := 1;

    tant que (non valeurDupliquee ET i <= nbElements) faire
        valeur := tab[i];
        j := 1;
        tant que (non valeurDupliquee ET j <= nbElements) faire
            valeurDupliquee := i != j ET valeur == tab[j];
            j := j + 1;
        
        i := i + 1;
    finfaire

    retourne valeurDupliquee;
fin