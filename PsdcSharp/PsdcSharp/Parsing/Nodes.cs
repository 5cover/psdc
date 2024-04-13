namespace Scover.Psdc.Parsing.Nodes;

internal interface Node
{
    internal sealed record Algorithm(IReadOnlyCollection<Declaration> Declarations) : Node;

    internal interface Declaration : Node
    {
        internal sealed record MainProgram(string ProgramName, IReadOnlyCollection<Statement> Block) : Declaration;

        internal sealed record Alias(string Name, Type Type) : Declaration;

        internal sealed record Constant(string Name, Expression Value) : Declaration;

        internal sealed record ProcedureDeclaration(ProcedureSignature Signature) : Declaration;

        internal sealed record ProcedureDefinition(ProcedureSignature Signature, IReadOnlyCollection<Statement> Block) : Declaration;

        internal sealed record FunctionDeclaration(FunctionSignature Signature) : Declaration;

        internal sealed record FunctionDefinition(FunctionSignature Signature, IReadOnlyCollection<Statement> Block) : Declaration;

        internal sealed record ProcedureSignature(string Name, IReadOnlyCollection<FormalParameter> Parameters) : Node;

        internal sealed record FunctionSignature(string Name, IReadOnlyCollection<FormalParameter> Parameters, Type ReturnType) : Node;

        internal sealed record FormalParameter(ParameterMode Mode, string Name, Type Type) : Node;
    }

    internal sealed record EffectiveParameter(ParameterMode Mode, Expression Value) : Node;

    internal interface Statement : Node
    {
        internal sealed record VariableDeclaration(IReadOnlyCollection<string> Names, Type Type) : Statement;

        internal sealed record Return(Expression Value) : Statement;

        internal sealed record Alternative(
            Alternative.Clause If,
            IReadOnlyCollection<Alternative.Clause> ElseIfs,
            Option<IReadOnlyCollection<Statement>> Else) : Statement
        {
            // Helper type, not a node on it own
            internal sealed record Clause(Expression Condition, IReadOnlyCollection<Statement> Block);
        }

        internal sealed record WhileLoop(Expression Condition, IReadOnlyCollection<Statement> Block) : Statement;
        internal sealed record DoWhileLoop(IReadOnlyCollection<Statement> Block, Expression Condition) : Statement;
        internal sealed record RepeatLoop(IReadOnlyCollection<Statement> Block, Expression Condition) : Statement;
        internal sealed record ForLoop(
            string VariantName,
            Expression Start,
            Expression End,
            Option<Expression> Step,
            IReadOnlyCollection<Statement> Block) : Statement;

        internal sealed record Assignment(string Target, Expression Value) : Statement;

        internal sealed record Ecrire(Expression Argument1, Expression Argument2) : Statement;

        internal sealed record Print(IReadOnlyCollection<Expression> Arguments) : Statement;

        internal sealed record Fermer(Expression Argument) : Statement;

        internal sealed record Lire(Expression Argument1, Expression Argument2) : Statement;

        internal sealed record Read(Expression Argument) : Statement;

        internal sealed record OuvrirAjout(Expression Argument) : Statement;

        internal sealed record OuvrirEcriture(Expression Argument) : Statement;

        internal sealed record OuvrirLecture(Expression Argument) : Statement;
    }

    internal interface Expression : Node
    {
        internal sealed record OperationBinary(Expression Operand1, TokenType Operator, Expression Operand2) : Expression;

        internal sealed record OperationUnary(TokenType Operator, Expression Operand) : Expression;

        internal sealed record BuiltinFdf(Expression Argument) : Expression;

        internal sealed record Call(string Name, IReadOnlyCollection<EffectiveParameter> ParameterList) : Expression;

        internal sealed record ComponentAccess(Expression Structure, string ComponentName) : Expression;

        internal sealed record Bracketed(Expression Expression) : Expression;

        internal sealed record ArraySubscript(Expression Array, IReadOnlyCollection<Expression> Indices) : Expression;

        internal sealed record Variable(string Name) : Expression;

        #region Literals

        internal abstract record Literal : Expression
        {
            private Literal(string value) => Value = value;
            public string Value { get; }

            internal sealed record True() : Literal("vrai");

            internal sealed record False() : Literal("faux");

            internal sealed record Character(string Value) : Literal(Value);

            internal sealed record Integer(string Value) : Literal(Value);

            internal sealed record Real(string Value) : Literal(Value);

            internal sealed record String(string Value) : Literal(Value);
        }

        #endregion Literals
    }

    internal interface Type : Node
    {
        internal sealed record String : Type;

        internal sealed record Array(Type Type, IReadOnlyCollection<Expression> Dimensions) : Type;

        internal sealed record Primitive(PrimitiveType Type) : Type;

        internal sealed record AliasReference(string Name) : Type;

        internal sealed record LengthedString(Expression Length) : Type;

        internal sealed record StructureDefinition(IReadOnlyCollection<Statement.VariableDeclaration> Components) : Type;
    }
}

#region Terminals

internal enum ParameterMode
{
    In,
    Out,
    InOut,
}

internal enum PrimitiveType
{
    Boolean,
    Character,
    Integer,
    File,
    Real,
}

#endregion Terminals
