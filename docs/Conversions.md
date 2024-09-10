# Conversions

This is a graph of the allowed conversions between types.

```mermaid
flowchart LR
booléen -.faux &rarr; 0<br>vrai &rarr; 1.-> entier
entier -.0 &rarr; faux<br>&ne;0 &rarr; vrai.-> booléen
caractère -.impl-defined.-> entier
entier -.impl-defined.-> caractère
chaîneN["chaîne(<var>N</var>)"] --> chaîne
chaîneN ==Larger string (<var>n</var> > 0)==> chaîneN2["chaîne(<var>N</var> + <var>n</var>) "]
entier --> réel
réel -.round towards zero .->entier
nomFichierLog
arrays["<em>any array</em>"]
structures["<em>any structure</em>"]
```

Link shape|Conversion kind
-|-
Solid|implicit
Dotted|explicit (with cast syntax)
Thick|assigment only

---

When a conversion is **impl-defined**, the result depends on the rules of the target language.

When a conversion's result isn't specified, identity is assumed.
