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
fin