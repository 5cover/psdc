# Standard Pseudocode

## Mot-clés réservés

## Valeurs

### Types

#### Primitifs

#### Composés

#### Alias de types

### Expressions

#### Littéraux

#### Opérateurs

##### Opérateurs arithmétiques

Arité | Opérateur | Description
-|-|-|-
2 | `+` | addition ou concaténation si les deux opérandes sont des chaînes
2 | `-` | soustraction
2 | `*` | multiplication
2 | `/` | division
2 | `%` | modulo
1 | `+` | plus unaire
1 | `-` | négation unaire

#### Opérateurs de comparaison

Arité | Opérateur | Description
-|-|-
2 | `==` | Égal à
2 | `!=` | Différent de
2 | `>` | Strictement supérieur à
2 | `<` | Strictement inférieur à
2 | `>=` | Supérieur ou égal à
2 | `<=` | Inférieur ou égal à

##### Opérateurs booléens

Arité | Opérateur | Description
-|-|-
1 | `NON` | NON
2 | `ET` | ET
2 | `OU` | OU

#### Précédence

### Variables

#### Déclarations

#### Constantes

### Affectation

## Fonctionnalités

### Programme principal

### Procédures et fonctions

#### Déclaration

#### Définition

#### Appel

### Sous-programmes prédéfinis

Sous-programmes représentant la bibliothèque standard.

Ils s'apparentent à des procédures ou des fonctions sauf que le mode de transmission n'a pas à être spécifié lors de l'appel.

#### `ecrireEcran`

**Alias** : `ecrire`

**Description** : affiche une représentation textuelle de ses paramètres à l'écran suivie d'un retour à la ligne.

**Paramètres** : variadique. Chaque paramètre est passé en entrée et leur représentations textuelles sont concaténées puis affichées.

**Retour** : rien

**Exemple** :

    age : entier;
    age := 10;
    ecrireEcran("Vous avez ", age, " ans.);
    // Affiche à l'écran "Vous avez 10 ans."

#### `lireClavier`

**Alias** : `lire`

**Description** : demande une entrée au clavier à l'utilisateur.

**Paramètres** :
Position | Mode de transmission | Description | Type
-|-|-|-
1 | sortie | Variable qui contiendra la valeur entrée après l'appel | N'importe lequel type primitif hors `booléen`

**Retour** : rien

**Exemple** :

    age : entier;
    lireClavier(age);
    // si l'utilisateur entre 5, age vaudra 5

### Structures

## Structures de contrôle

### Alternatives

    si (condition) alors
        /*
        statements
        */
    finsi

*condition* : expression de type `booléen`

#### Syntaxe

Ordre | Mot-clé | Description | Occurences
-|-|-|-
1 | `si` | Débute l'alternative | 1 fois
2 | `sinon si` | Définit des conditions additionnelles | 0, 1, ou plusieurs fois.
3 | `sinon` | Définit un bloc a exécuter quand toutes les conditions ont été évaluées à ``faux`` | 0 ou 1 fois.
4 | `finsi` | Termine l'alternative. Présent 1 fois.

#### Exemples

    si (condition1) alors
        /*
        bloc exécuté si condition1 est évaluée à vrai
        */
    sinon si (condition2) alors
        /*
        bloc exécuté si condition1 est évaluée à faux et condition2 est évaluée à vrai
        */
    sinon
        /*
        bloc exécuté si condition1 est évaluée à faux et condition2 est évaluée à faux
        */
    finsi

### Boucles

#### `tant que`

#### `faire ... tant que`

#### `pour`

## Commentaires

### Sur une ligne

Début : `//`

    // commentaire

### Multiligne

Début : `/*`

Fin : `*/`

    /* commentaire
    multiligne */
