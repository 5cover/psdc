/*
Ex 4 TD9 dév.
*/

/*
1. Proposez une définition pour les types de ces 2 tableaux
*/

constante entier NB_VILLES := 5;

type t_tableau_villes = tableau[NB_VILLES] de entier;

type t_tableau_distances = tableau[NB_VILLES][NB_VILLES] de entier;

constante t_tableau_villes villes = {"Nantes", "Rennes", "Lannion", "Brest", "Vannes"};

/*
2. Programme demandant deux noms de ville, et affichant, si possible la distance entre ces deux villes. On supposera qu'il existe la procédure remplirDistance() qui complétera les différentes distances entre les villes.
*/

/*
2.a La fonction qui retourne l'indice de la ville dans le tableau ou -1 en cas d'échec.
*/
fonction rechercheVille(entF nomVille : chaîne) délivre entier c'est
début
    i : entier;
    pour i de 1 à NB_VILLES faire
        si villes[i] = nomVille alors
            retourne i;
        finsi
    finfaire
    retourne -1;
fin

/*
2.b La fonction qui retourne la distance entre deux villes
*/
fonction rechercheDistance(entF villeDepart : entier, entF villeArrivee : entier, entF distances : t_tableau_distances) délivre entier c'est
début
    retourne distances[villeDepart][villeArrivee];
fin

programme ex4 c'est
début
    nomVille1, nomVille2 : chaîne(64);
    distance, iVille1, iVille2 : entier;
    distances : t_tableau_distances;

    remplirDistance(sortE distances);

    faire
        écrireEcran("Nom ville 1 : ");
        lireClavier(nomVille1);
        iVille1 = rechercheVille(entE nomVille1)
        si iVille1 == -1 alors
            écrireEcran("Ville inconnue");
    tant que (iVille1 == -1)

    faire
        écrireEcran("Nom ville 2 : ");
        lireClavier(nomVille2);
        iVille2 = rechercheVille(entE nomVille2)
        si iVille2 == -1 alors
            écrireEcran("Ville inconnue");
    tant que (iVille2 == -1)

    distance := rechercheDistance(entE iVille1, entE iVille2, entE distances);

    écrireEcran("La distance entre ces deux villes est ", distance);
fin