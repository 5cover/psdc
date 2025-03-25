programme eligibilitePermis c'est

fonction lireBool() délivre booléen c'est début
    c : caractère;
    c := 'n';
    lireClavier(c);
    retourne c == 'o';
fin

début
    estMajeur, aLeCode, aAssezDeLecons, aUnAnDePratique : booléen;

    écrireEcran("Êtes-vous majeur? (o/n) ");
    estMajeur := lireBool();
    écrireEcran("Avez-vous obtenu le code? (o/n) ");
    aLeCode := lireBool();
    écrireEcran("Avez-vous fait au moins 21h de leçons de conduite? (o/n) ");
    aAssezDeLecons := lireBool();
    écrireEcran("Avez-vous une année de pratique? (o/n) ");
    aUnAnDePratique := lireBool();

    écrireEcran("Droit de passer le permis de conduire : ",
        estMajeur et aLeCode et aAssezDeLecons et aUnAnDePratique);
fin
