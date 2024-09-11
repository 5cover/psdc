/*
Ex 2 TD8 dév.
*/

/*
1. Proposez une procédure pour afficher un livre
*/
procédure afficher(entF livre : t_livre) c'est
début
    écrireEcran("Livre : ");
    écrireEcran("\tRéférence : ", livre.c_ref);
    écrireEcran("\tTitre     : ", livre.c_titre);
    écrireEcran("\tAuteur    : ", livre.c_auteur);
    écrireEcran("\tStatut    : ");
    si (livre.c_present) alors
        écrireEcran("présent");
    sinon
        écrireEcran("emprunté");
    finsi
fin

/*
2. Proposez une fonction pour saisir un livre
*/
fonction saisir() délivre t_livre c'est
début
    livre : t_livre;

    écrireEcran("Référence :");
    lireClavier(livre.c_ref);
    écrireEcran("Titre :");
    lireClavier(livre.c_titre);
    écrireEcran("Auteur :");
    lireClavier(livre.c_auteur);
    écrireEcran("Statut :");
    lireClavier(livre.c_present);

    retourne livre
fin

/*
3. Proposer une procédure pour insérer un livre dans une bibliothèque et le programme de test associé
*/
procédure insérer(entF livre : t_livre, entF/sortF bibliothèque : t_bib) c'est
    si (bibliothèque.c_nbre >= MAXL) alors
        écrireEcran("Nombre maximum de livres atteint dans la bibliothèque (", MAXL, ").");
    sinon
        bibliothèque.c_nbre := bibliothèque.c_nbre + 1;
        bibliothèque.c_contenu[bibliothèque.c_nbre] = t_livre;
    finsi
début

// Test de la fonction
programme ex2 c'est
début
    bPleine : t_bib;
    bDéborde : t_bib;
    bOk : t_bib;
    bVide : t_bib;
    bPleinMoinsUn : t_bib;
    livre : t_livre;
    succes : booléen;
    ancienNombre : entier;

    bPleine.c_nbre = MAXL;
    bDéborde.c_nbre = MAXL + 5;
    bOk.c_nbre = 5;
    bVide.c_nbre = 0;
    bPleinMoinsUn.c_nbre = MAXL - 1;

    livre.c_ref := 4282006;
    livre.c_titre := "Moby Dick";
    livre.c_auteur := "Herman Melville";
    livre.c_present := vrai;

    succès := vrai;

    // Cas 1 : erreur : bibliothèque pleine
    insérer(entE livre, entE/sortE bPleine);
    // le test réussit si le message s'affiche

    // Cas 2 : erreur : bibliothèqe qui déborde (cela ne devrait pas arriver)
    insérer(entE livre, entE/sortE bDéborde);
    // le test réussit si le message s'affiche

    // Cas 3 : nominal : bibliothèque OK
    ancienNombre := bOk.c_nbre;
    insérer(entE livre, entE/sortE bOk);
    succès := succès
     ET bOk.c_nbre == ancienNombre + 1
     ET bOk.c_contenu[bOk.c_nbre].c_ref == livre.c_ref;

    // Cas 4 : limite : bilibothèque vide
    ancienNombre := bVide.c_nbre;
    insérer(entE livre, entE/sortE bVide);
    succès := succès
     ET bVide.c_nbre == ancienNombre + 1
     ET bVide.c_contenu[bVide.c_nbre].c_ref == livre.c_ref;
    
    // Cas 5 : limite : bibliothèque pleine moins un
    ancienNombre := bPleinMoinsUn.c_nbre;
    insérer(entE livre, entE/sortE bPleineMoinsUn);
    succès := succès
     ET bPleinMoinsUn.c_nbre == ancienNombre + 1
     ET bPleineMoinsUn.c_contenu[bPleineMoinsUn.c_nbre].c_ref == livre.c_ref;

    écrireEcran("Réussite : ", succès);
fin

/* 4. Proposer une procédure pour afficher la liste des livres d'une bibliothèque */
procédure aff(entF bib : t_bib) c'est
début
    pour i de 1 à bib.c_nbre faire
        afficher(entE bib.c_contenu[i]);
    finfaire
fin

/* 5. Proposer une procédure pour enregistrer l'emprunt d'un livre de la bibliothèque. La procédure inclura la saisie de la référence ref du livre permettant de l'identifier */
procédure enregisterEmprunt(entF bib : t_bib) c'est
début
    ref : entier;
    livreTrouve : booléen;
    i : entier;

    écrireEcran("Emprunt d'un livre : entrer la référence : ");
    lireClavier(ref);

    livreTrouve := faux;
    i := 1;
    // Rechercher le livre dans la bibliothèque
    tant que non livreTrouvé ET i <= MAXL faire 
        si (bib.c_contenu[i].c_ref == ref) alors
            livreTrouve := vrai;
            bib.c_contenu[i].c_present := faux;
            écrireEcran("Emprunt du livre numéro ", i, " enregistré.");
        finsi
        i := i + 1;
    finfaire

    si non livreTrouvé alors
        écrireEcran("Livre non trouvé dans la bibliothèque");
    finsi
fin

/* 6.a Proposez un type t_lecteur décrivant un lecteur sous la forme d'une structure avec un numéro, un nom, un prénom ainsi qu'un tableau des livres empruntés par le lecteur et le nombre d'emprunts en cours */

type t_lecteur = structure début
    c_num : entier;
    c_prenom : chaîne(64);
    c_nom : chaîne(64);
    c_livresEmpruntés : tableau[MAXL] de t_livre;
    c_nbEmprunts : entier;
fin

constante entier MAX_LECTEURS := 256;

/* 6.b définissez un type t_LesLecteurs décrivant les lecteurs d'une bibliothèque à l'aide d'une structure contenant un tableau de t_lecteur ainsi que le nombre de lecteurs */

type t_LesLecteurs = structure début
    c_lecteurs : tableau[MAX_LECTEURS];
    c_nbLecteurs : entier;
fin

/* 6.c modifiez le type t_bib décrivant une bibliothèque pour intégrer les lecteurs (nouvelle rubrique) */

type t_bib = structure début
    c_contenu : t_TabLivres;
    c_lecteurs : t_LesLecteurs;
fin

/* 7. */

procédure ajouterLecteur(entF lecteur : t_lecteur, entF/sortF bib : t_bib, sortF numLec : entier) c'est
début
    si bib.c_lecteurs.c_nbLecteurs >= MAX_LECTEURS alors
        écrireEcran("Nombre maximum de lecteurs atteint");
    sinon
        numLec := bib.c_lecteurs.c_lecteurs[bib.c_lecteurs.c_nnbLecteurs].c_num + 1;
        // todo
    finsi

    /* Limites de ce mode de calcul */
    // Il y a une erreur à gérer si bib.c_lecteurs.c_nbLecteurs >= MAX_LECTEURS
fin

/* 8. */

procédure enregisterEmprunt(entF lecteur : t_lecteur, entF bib : t_bib) c'est
début
    ref : entier;
    empruntFait : booléen;
    i : entier;
    iLecteur : entier;

    écrireEcran("Emprunt d'un livre : entrer la référence : ");
    lireClavier(ref);

    empruntFait := faux;
    i := 1;
    // Rechercher le livre dans la bibliothèque
    tant que non empruntFait ET i <= MAXL faire 
        si (bib.c_contenu[i].c_ref == ref) alors
            // Rechercher le lecteur
            iLecteur := 1;
            tant que non empruntFait et iLecteur <= bib.c_lecteurs.c_nbLecteurs faire
                si (bib.c_lecteurs.c_lecteurs[i].c_num == lecteur.c_num) alors
                    empruntFait := vrai;
                    écrireEcran("Emprunt du livre numéro ", i, " enregistré au lecteur ", iLecteur, ".");
                    // TODO : vérifier si on a pas dépassés le maximum de livre empruntés
                    bib.c_lecteurs.c_lecteurs[i].c_nbEmprunts = bib.c_lecteurs.c_lecteurs[i].c_nbEmprunts + 1;
                    bib.c_lecteurs.c_lecteurs[i].c_livresEmpruntes[bib.c_lecteurs.c_lecteurs[i].c_nbEmprunts] := bib.c_contenu[i];
                iLecteur := iLecteur + 1;
            finfaire
            bib.c_contenu[i].c_present := faux;
        finsi
        i := i + 1;
    finfaire

    si non empruntFait alors
        écrireEcran("Livre non trouvé dans la bibliothèque");
    finsi
fin

/* 9. */

procédure supprimerLecteur(entF lecteur : t_lecteur, entF/sortF bib : t_bib) c'est
début
    si bib.c_lecteurs.c_nbLecteurs <= 0 alors
        écrireEcran("Aucun lecteur");
    sinon
        bib.c_lecteurs.c_nbLecteurs := bib.c_lecteurs.c_nbLecteurs - 1;
    finsi
fin
