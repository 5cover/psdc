programme moyenneAlgorithmique c'est
début
    noteDS1, noteDS2, noteTP, moyenneDS, moyenneGenerale : réel;

    écrireEcran("Note DS1 : ");
    lireClavier(noteDS1);
    écrireEcran("Note DS2 : ");
    lireClavier(noteDS2);
    écrireEcran("Note TP : ");
    lireClavier(noteTP);

    moyenneDS := (noteDS1 + noteDS2 * 3) / 4;
    moyenneGenerale := moyenneDS * (2 / 3.0) + noteTP * (1 / 3.0);

    écrireEcran("La moyenne de DS est de : ", moyenneDS," la moyenne générale est de ", moyenneGenerale, ".");
fin