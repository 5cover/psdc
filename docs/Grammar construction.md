# Grammar

Formal grammar.

Production rule descriptions.

Grammar units of the language.

Determines the nodes of the abstract syntax tree.

The tree is created by the parser and used for code generation.

Brainstroming:

- SyntaxNode
- Expression
- Call : Expression
    - string symbolName
    - IEnumerable\<EffectiveParameter> effectiveParameters
- Literal\<T> : Expression
    - T value
- Operation : Expression
    - Operator Operator
    - IReadOnlyList\<Expression>

Non-node types:

- Parameter
    - ParameterMode node
    - string name
    - Type type
- enum ParameterMode
    - In
    - Out
    - InOut
- Type

Visitor pattern to convert to C and French ?

Fill-A-Hole(TM)s for grammar description :

`<space>`: whitespace required
`*` : zero or more of

name|pattern|arg1|arg2|arg3
-|-|-|-|-
DeclarationVariable|`{name}:{type}`|variable name|variable type
DeclarationProcedure|`procédure {name}({{mode} {paramName}:{type}}*);`|procedure name|parameter list
TypeAlias|`type {name}={type};`|alias name|alias type

What is this, is this useful?

name|sequence
-|-
DeclarationFunction|"FunctionSignature" DelimiterTerminator
DeclarationProcedure|"ProcedureSignature" DelimiterTerminator
DeclarationVariable|Identifier DelimiterColon Identifier
DefinitionFunction|"FunctionSignature" KeywordIs
DefinitionProcedure|"ProcedureSignature" KeywordIs
ExpressionBinaryOperation|Identifier Operator Identifier
ExpressionFunctionCall|Identifier OpenBracket (ParameterMode Identifier)* CloseBracket
ExpressionLiteralInteger|LiteralInteger
ExpressionUnaryOperation|Operator Identifier
SignatureFunction|KeywordFunction Identifier OpenBracket (ParameterMode Identifier DelimiterColon Identifier)* CloseBracket KeywordDelivers Identifier
SignatureProcedure|KeywordProcedure Identifier OpenBracket (ParameterMode Identifier DelimiterColon Identifier)* CloseBracket
