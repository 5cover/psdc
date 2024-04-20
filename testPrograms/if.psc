programme VotreAge3000 c'est
début
    age : entier;
    écrireÉcran("Quel âge avez-vous ? ");
    lireClavier(age);

    écrireÉcran("Vous avez ", age, " ans.");

    si age >= 18 alors
        écrireÉcran("Vous êtes majeur");
    sinonsi age == 16 alors
        écrireÉcran("C'est l'heure de se faire recenser!");
    sinon
        écrireÉcran("T'es un bébé toi!");
    finsi

    si age < 0 alors
        écrireEcran("Votre âge est négatif? WTF");
    finsi

    si age >= 7 et age <= 77 alors
        écrireEcran("De 7 à 77 ans");
    sinon
        écrireEcran("Pas dans le range");
    finsi
fin