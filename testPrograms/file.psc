programme FileExample c'est
// Example of file management
début
    f : nomFichierLog;

    s : chaîne(50);

    assigner(f, "data.txt");

    ouvrirÉcriture(f);
    écrire(f, "bonjour");
    fermer(f);

    ouvrirLecture(f);
    lire(f, s);

    si (FdF(f)) alors
        écrireEcran("end of file");
    sinon
        écrireEcran("not end of file");
    finsi

    fermer(f);
fin