/*
Ex. 2.1.a TD7 Dév

Proposer en langage algorithmique la définition d'une procédure permettant de saisir au clavier toutes les valeurs de la variable tableau tab.
On utilisera pour cela une boucle "pour".

*/

programme tableaux c'est

constante entier MAX := 100;

procédure remplirTableau(sortF tab : tableau[MAX] de entier) c'est
début
    i : entier;
    pour i de 1 à MAX pas 1 faire
        écrireEcran("Entrer la valeur ", i, " : ");
        lireClavier(tab[i]);
    finfaire
fin