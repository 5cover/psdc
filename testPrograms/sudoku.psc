/*
Raphaël BARDINI - 1C2

SAÉ 1.01 - Algorithme programme principal Sudoku
*/


type t_grilleEntier = tableau[COTE_GRILLE][COTE_GRILLE] de entier;

programme Sudoku c'est
// Un jeu de Sudoku

    constante entier N := 3;
    constante entier NB_FICHIERS_GRILLES := 10;
    constante entier LONGUEUR_MAX_COMMANDE := 64;
    constante entier COTE_GRILLE := N * N;

début
    grilleJeu : t_grilleEntier;
    ligneCurseur, colonneCurseur : entier;
    partieAbandonnée : booléen;
    commandeRéussie : booléen;
    commande : chaîne(LONGUEUR_MAX_COMMANDE);

    partieAbandonnée := faux;
    ligneCurseur := 1;
    colonneCurseur := 1;

    chargerGrille(entE entierAléatoire(entE 1, entE NB_FICHIERS_GRILLES), sortE grilleJeu);

    // Boucle principale du jeu
    faire
        écrireGrille(entE grilleJeu);
        faire
            commande := entréeCommande();
            commandeRéussie := exécuterCommande(entE commande,
                                                entE/sortE grilleJeu,
                                                entE/sortE ligneCurseur,
                                                entE/sortE colonneCurseur,
                                                sortE partieAbandonnée);
        tant que (non commandeRéussie)
    tant que (non partieAbandonnée ET non estGrilleComplète(entE grilleJeu))

    // La partie n'a pas été abandonnée, elle s'est donc terminée par une victoire
    si (non partieAbandonnée) alors
        écrireEcran("Bravo, vous avez gagné !");
    finsi
fin