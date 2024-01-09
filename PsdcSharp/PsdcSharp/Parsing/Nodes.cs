namespace Scover.Psdc.Parsing.Nodes;

internal interface Node
{
    internal sealed record Algorithm(IReadOnlyCollection<ParseResult<Declaration>> Declarations) : Node;

    internal interface Declaration : Node
    {
        internal sealed record MainProgram(ParseResult<string> ProgramName, IReadOnlyCollection<ParseResult<Statement>> Block) : Declaration;

        internal sealed record Alias(ParseResult<string> Name, ParseResult<Type> Type) : Declaration;

        internal sealed record Constant(ParseResult<string> Name, ParseResult<Expression> Value) : Declaration;

        internal sealed record ProcedureDeclaration(ParseResult<ProcedureSignature> Signature) : Declaration;

        internal sealed record ProcedureDefinition(ParseResult<ProcedureSignature> Signature, IReadOnlyCollection<ParseResult<Statement>> Block) : Declaration;

        internal sealed record FunctionDeclaration(ParseResult<FunctionSignature> Signature) : Declaration;

        internal sealed record FunctionDefinition(ParseResult<FunctionSignature> Signature, IReadOnlyCollection<ParseResult<Statement>> Block) : Declaration;
    }

    internal sealed record ProcedureSignature(ParseResult<string> Name, IReadOnlyCollection<ParseResult<FormalParameter>> ParameterList) : Node;

    internal sealed record FunctionSignature(ParseResult<string> Name, IReadOnlyCollection<ParseResult<FormalParameter>> ParameterList, Type ReturnType) : Node;

    internal sealed record FormalParameter(ParseResult<ParameterMode> Mode, ParseResult<string> Name, ParseResult<Type> ParameterType) : Node;

    internal sealed record EffectiveParameter(ParseResult<ParameterMode> Mode, ParseResult<Expression> Value) : Node;

    internal interface Statement : Node
    {
        internal sealed record VariableDeclaration(IReadOnlyCollection<ParseResult<string>> Names, ParseResult<Type> Type) : Statement;

        internal sealed record Return(ParseResult<Expression> Value) : Statement;

        internal sealed record Alternative(
            Alternative.Clause If,
            IReadOnlyCollection<ParseResult<Alternative.Clause>> ElseIfs,
            Option<IReadOnlyCollection<ParseResult<Statement>>> Else) : Statement
        {
            // Helper type, not a node on it own
            internal sealed record Clause(ParseResult<Expression> Condition, IReadOnlyCollection<ParseResult<Statement>> Block);
        }

        internal sealed record Assignment(ParseResult<string> Target, ParseResult<Expression> Value) : Statement;

        internal sealed record Ecrire(ParseResult<Expression> Argument1, ParseResult<Expression> Argument2) : Statement;

        internal sealed record Print(IReadOnlyCollection<ParseResult<Expression>> Arguments) : Statement;

        internal sealed record Fermer(ParseResult<Expression> Argument) : Statement;

        internal sealed record Lire(ParseResult<Expression> Argument1, ParseResult<Expression> Argument2) : Statement;

        internal sealed record Read(ParseResult<Expression> Argument) : Statement;

        internal sealed record OuvrirAjout(ParseResult<Expression> Argument) : Statement;

        internal sealed record OuvrirEcriture(ParseResult<Expression> Argument) : Statement;

        internal sealed record OuvrirLecture(ParseResult<Expression> Argument) : Statement;
    }

    internal interface Expression : Node
    {
        internal sealed record OperationBinary(ParseResult<Expression> Operand1, TokenType Operator, ParseResult<Expression> Operand2) : Expression;

        internal sealed record OperationUnary(TokenType Operator, ParseResult<Expression> Operand) : Expression;

        internal sealed record BuiltinFdf(ParseResult<Expression> Argument) : Expression;

        internal sealed record Call(ParseResult<string> Name, IReadOnlyCollection<ParseResult<EffectiveParameter>> ParameterList) : Expression;

        internal sealed record ComponentAccess(ParseResult<Expression> Structure, ParseResult<string> ComponentName) : Expression;

        internal sealed record Bracketed(ParseResult<Expression> Expression) : Expression;

        internal sealed record ArraySubscript(ParseResult<Expression> Array, IReadOnlyCollection<ParseResult<Expression>> Indices) : Expression;

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

        internal sealed record Array(ParseResult<Type> Type, IReadOnlyCollection<ParseResult<Expression>> Dimensions) : Type;

        internal sealed record Primitive(PrimitiveType Type) : Type;

        internal sealed record AliasReference(ParseResult<string> Name) : Type;

        internal sealed record LengthedString(ParseResult<Expression> Length) : Type;

        internal sealed record StructureDefinition(IReadOnlyCollection<ParseResult<Statement.VariableDeclaration>> Components) : Type;
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
