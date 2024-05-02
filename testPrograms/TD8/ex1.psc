/*
Ex. 1 TD7 Dév.
*/

type t_candidat = structure
début
    c_nom: chaîne(64);
    c_age: entier;
    c_nbVoix: entier;
fin

fonction saisie() délivre t_candidat c'est
// saisie d'un candidat
début
    t_candidat : candidat;

    écrireEcran("Nom du candidat : ");
    lireClavier(t_candidat.c_nom);
    écrireEcran("Âge du candidat : ");
    lireClavier(t_candidat.c_age);
    écrireEcran("Nombre de voix : ");
    lireClavier(t_candidat.c_nbVoix);
    
    retourne candidat;
fin

fonction compare(entF c1 : t_candidat, entF c2 : t_candidat) délivre entier c'est
// comparaison de deux candidats
// Retour :
// -1 si c1 < c2
// 0 si c1 = c2
// 1 si c1 > c2 
début
    ordre : entier;

    ordre := 0;

    // Trier par
    // 1. nbVoix
    // 2. age (décroissant)

    si (c1.c_nbVoix < c2.c_nbVoix) alors
        ordre := -1;
    sinonsi (c1.c_nbVoix > c2.c_nbVoix) alors
        ordre := 1;
    sinon
        si (c1.c_age < c2.c_age) alors
            ordre := 1;
        sinonsi (c1.c_age > c2.c_age) alors
            ordre := -1;
    finsi

    retourne ordre;
fin

programme ex1 c'est
début
    candidat1, candidat2 : t_candidat;
    ordre : entier;

    écrireEcran("Premier candidat : ");
    candidat1 := saisie();
    écrireEcran("Second candidat : ");
    candidat2 := saisie();

    ordre := compare(entE candidat1, entE candidat2);
    
    /*selon gagnant c'est
        quand -1 => statement
        quand 1 => statement
        quand 0 => statement
        quand autre => statement
    finSelon*/

    si (ordre == -1) alors
        écrireEcran("Le premier candidat a gagné");
    sinonsi (ordre == 1) alors
        écrireEcran("Le second candidat a gagné");
    sinon
        écrireEcran("Égalité entre les deux candidats");
    finsi
fin