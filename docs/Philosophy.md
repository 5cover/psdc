# Philosophy

## Pseudocode

Pseudocode is made for semantics. We have no runtime constraints since the code will never run. So we should choose the approach that offers the best semantics.

It's acceptable to be non-deterministic (code that gives a different result depending on the target language) but be so **explicitly** so bug-prone code stands out.

## Psdc

Psdc was designed to automate the painstaking task of converting Pseudocode to other languages.

It will not assume the intent of the code or write opiniated code that adheres to specific solutions.

Therefore, Psdc will not allow anything that the target language doesn't. For C, this can be:

- Printing/Inputting booleans (`booléen` type)
- Printing/Inputting files (`nomFichierLog` type)

Of course, you can circumvent these limitations by implementing the logic yourself. Example:

```text
b : booléen;

// écrireEcran(b); // Forbidden - it is unclear what the desired output is
// unless the target language provides a default way to print booleans.

si (b) alors
    écrireEcran("vrai");
sinon
    écrireEcran("faux");
finsi
```
