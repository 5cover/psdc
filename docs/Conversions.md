# Conversions

This is a graph of the allowed implicit and explicit conversions between types.

Legend:

- Dotted link: implicit conversion
- Solid link: explicit conversion (with cast syntax)

```mermaid
flowchart LR
booléen -.faux &rarr; 0<br>vrai &rarr; 1.-> entier
caractère -.impl-defined.-> entier
entier -.impl-defined.-> caractère
chaîneN["chaîne(<var>N</var>)"] --> chaîne
entier --> réel
réel -.truncation.->entier
nomFichierLog
arrays["<em>any array</em>"]
structures["<em>any structure</em>"]
```

When the result of a conversion is **impl-defined**, it means it depends on the rules of the target language.

When the result of a conversion isn't mentioned, identity is assumed.
