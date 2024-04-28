# PSDC limitations

PSDC was designed to automate the painstaking task of converting code between languages with a different syntax.

It will not assume the your intent or write opiniated code that adheres to specific solutions.

Therefore PSDC will not allow the following :

- Printing/Inputting booleans (`booléen` type)
- Printing/Inputting files (`nomFichierLog` type)

Of course, you can circumvent these limitations by implementing the logic yourself. Example :

```text
b : booléen;

// écrireEcran(b); // Forbidden - it is unclear what the desired output is

si (b) alors
    écrireEcran("vrai");
sinon
    écrireEcran("faux");
finsi
```
