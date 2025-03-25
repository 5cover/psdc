constante entier N := 30;

type t_fct = structure début
    c_nomF : chaîne(N);
    c_nbParametres : entier;
fin

constante entier MAXF := 20;

type t_tabFct := tableau [MAXF] de t_fct;
type t_pile = structure début
    c_pile : t_tabFct;
    c_ip : entier; // indice de la fonction située au sommet de la pile représentée par le tableau pile
fin

/*
1. Proposez l'écriture des procédures et fonctions ci-dessous pour gérer cette pile de fonctions.
*/
/* a. Question  : Fonction init() permettant la création et l'initialisation d'une pile. */
fonction init() : t_pile délivre t_pile c'est début
    pile : t_pile;
    pile.c_ip := 0;
    retourne pile;
fin

/* b. Question  : Fonction pileVide() retournant vrai si la pile est vide, faux sinon. */
fonction pileVide(entF p : t_pile) : booléen délivre booléen c'est début
    retourne p.c_ip == 0;
fin

/* c. Question  : Procédure depiler() permettant de supprimer une fonction f dans une pile p, charge à cette procédure de vérifier si la pile p n'est pas vide et d'en informer l'utilisateur le cas échéant. Si la fonction dépilée est le «  main  », la procédure vérifiera que la pile est désormais vide. Dans ce cas, elle affichera le message « exécution terminée ». Dans le cas inverse elle affichera « erreur d'exécution ».*/
procédure depiler(entF/sortF p : t_pile) début
    si pileVide(entF p) alors
        afficher("erreur : pile vide");
    sinon
        tete : t_fct;
        tete := p.c_pile[p.c_ip];
        p.c_ip := p.c_ip - 1;

        si tete.c_nomF == "main" alors
            si pileVide(entF p) alors
                afficher("exécution terminée");
            sinon
                afficher("erreur d'exécution");
            finsi
        finsi
    finsi
fin

/* d. Question  : Procédure fonctionsEnCours() permettant d'afficher le nom de la fonction en cours d’exécution */
procédure fonctionsEnCours(entF p : t_pile) début
    tete : t_fct;
    tete := p.c_pile[p.c_ip].c_nomF;
    écrireEcran(tete);
fin