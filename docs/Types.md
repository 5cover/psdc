# Types in Psdc

```text
    Node.Type -------\
                     t-> TypeInfo
    Node.Expression -/       string CreateDeclaration(string identifier)
                             string ToString()
                             bool IsNumeric { get; }
```

We have two different sources of types : types directly expressed in code code and types resulting from the evaluation of expressions.

The difference is that types expressed in code have their characteristics encapsulated in ParseResults, whereas for inferred types we get the values directly.

This calls for two different types of types, DeclaredType and InferredType. But how to define them so we don't duplicate their internal structure ?

What is a type to begin with?

Is it a discriminated union? A class?

## Use cases

### Creation

- Create from NodeType
- Evaluate type of expression

### Usage

- Get format component (Option)
- Create declarations
    - variable
    - constant
    - return type
    - parameter
    - structure component
    - typedef

## Examples

### Psdc

```psdc
    fonction somme(entF a : entier, entF b : entier) délivre entier c'est
    début
        retourne a + b;
    fin

    variable : entier; //-> Node.Statement.VariableDeclaration("variable", Node.Type.Primitive(PrimitiveType.Integer))  
    écrire(variable);

    écrire(5 + 6);

    v : chaîne(12);
    écrire(v);

    écrire("Bonjour");
```

### C

```psdc
    int variable;
    printf("%d", variable);

    printf("%d", 5 + 6);

    char v[12];
    printf("%s", v);

    printf("%s", "Bonjour");
    // ou
    printf("Bonjour");
```

## Kinds of types

- Primitives
    - Integer
    - Real
    - Character
    - File
    - Boolean
- String
- String(Length)
- Array(Type, Dimensions)
- Alias(Name)
- Structure (Components)

About structure types : they cannot directly be used as types, they must be first declared using a type alis (typedef)

A type alias is basically a shortcut to a type, except in the case of structs that are not types but definitions.

A StructDefinition.

/!\ Attention au chaînes contenant des séquences nécéssitant un échappement ou '%' pour printf.
