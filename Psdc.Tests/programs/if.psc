programme VotreAge3000 c'est

constante entier ANNEE_ACTUELLE := 2024;

début
    age : entier;
    écrireÉcran("Quel âge avez-vous ? ");
    lireClavier(age);

    écrireÉcran("Vous avez ", age, " ans.");

    si (age >= 18) alors
        écrireÉcran("Vous êtes majeur");
    sinonsi (age == 16) alors
        écrireÉcran("C'est l'heure de se faire recenser!");
    sinonsi age == 18 alors
    sinon
        écrireÉcran("T'es un bébé toi!");
    finsi

    si (age < 0) alors
        écrireEcran("Votre âge est négatif? WTF");
    finsi

    si (age >= 7 ET age <= 77) alors
        écrireEcran("De 7 à 77 ans");
        si (age % 2 == 0) alors
            écrireEcran("age pair");
            si (ANNEE_ACTUELLE - age % 4 == 0) alors
                écrireEcran("vous êtes né lors d'une année bissextile");
            finsi
        sinon
            écrireEcran("age impair");
        finsi
    sinon
        écrireEcran("Pas dans le range");
    finsi

    selon age c'est
        quand 1 =>
            écrireEcran("Vous avez ");
            écrireEcran("un");
            selon age c'est
                quand 1 =>
                    écrireEcran("Vous avez ");
                    écrireEcran("un");
                    écrireEcran(" an");
                quand 0 => écrireEcran("Ton âge est nul");
                quand 45 + 18 - 0 * 1 => écrireEcran("t sacrément vieux");
                quand autre => écrireEcran("je sais même pas koi dire °_°");
            finselon
            écrireEcran(" an");
        quand 0 => écrireEcran("Ton âge est nul");
        quand 45 + 18 - 0 * 1 => écrireEcran("t sacrément vieux");
        quand autre => écrireEcran("je sais même pas koi dire °_°");
    finselon
fin