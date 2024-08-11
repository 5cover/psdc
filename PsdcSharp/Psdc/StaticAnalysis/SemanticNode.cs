global using SemanticBlock = System.Collections.Generic.IReadOnlyList<Scover.Psdc.StaticAnalysis.SemanticNode.Statement>;

using Scover.Psdc.Language;
using Scover.Psdc.Parsing;
using static Scover.Psdc.StaticAnalysis.SemanticNode;

namespace Scover.Psdc.StaticAnalysis;

public readonly record struct SemanticMetadata(Scope Scope, SourceTokens SourceTokens);

interface SemanticCallNode : SemanticNode
{
    public Identifier Name { get; }
    public IReadOnlyList<ParameterActual> Parameters { get; }
}

interface SemanticBracketedExpressionNode : Expression
{
    public Expression ContainedExpression { get; }
}

public interface SemanticNode
{
    SemanticMetadata Meta { get; }

    public sealed record Algorithm(SemanticMetadata Meta,
        Identifier Name,
        IReadOnlyList<Declaration> Declarations)
    : SemanticNode;

    public interface Declaration : SemanticNode
    {
        internal sealed record MainProgram(SemanticMetadata Meta,
            SemanticBlock Block)
        : Declaration;

        internal sealed record TypeAlias(SemanticMetadata Meta,
            Identifier Name,
            EvaluatedType Type)
        : Declaration;

        internal sealed record Constant(SemanticMetadata Meta,
            EvaluatedType Type,
            Identifier Name,
            Expression Value)
        : Declaration;

        internal sealed record Procedure(SemanticMetadata Meta,
            ProcedureSignature Signature)
        : Declaration;

        internal sealed record ProcedureDefinition(SemanticMetadata Meta,
            ProcedureSignature Signature,
            SemanticBlock Block)
        : Declaration;

        internal sealed record Function(SemanticMetadata Meta,
            FunctionSignature Signature)
        : Declaration;

        internal sealed record FunctionDefinition(SemanticMetadata Meta,
            FunctionSignature Signature,
            SemanticBlock Block)
        : Declaration;
    }

    internal interface Statement : SemanticNode
    {
        internal sealed record Nop(SemanticMetadata Meta)
        : Statement;

        internal sealed record Alternative(SemanticMetadata Meta,
            Alternative.IfClause If,
            IReadOnlyList<Alternative.ElseIfClause> ElseIfs,
            Option<Alternative.ElseClause> Else)
        : Statement
        {
            internal sealed record IfClause(SemanticMetadata Meta,
                Expression Condition,
                SemanticBlock Block)
            : SemanticNode;

            internal sealed record ElseIfClause(SemanticMetadata Meta,
                Expression Condition,
                SemanticBlock Block)
            : SemanticNode;

            internal sealed record ElseClause(SemanticMetadata Meta,
                SemanticBlock Block)
            : SemanticNode;
        }

        internal sealed record Switch(SemanticMetadata Meta,
            Expression Expression,
            IReadOnlyList<Switch.Case> Cases,
            Option<Switch.DefaultCase> Default)
        : Statement
        {
            internal sealed record Case(SemanticMetadata Meta,
                Expression Value,
                SemanticBlock Block)
            : SemanticNode;

            internal sealed record DefaultCase(SemanticMetadata Meta,
                SemanticBlock Block)
            : SemanticNode;
        }

        internal sealed record Assignment(SemanticMetadata Meta,
            Expression.Lvalue Target,
            Expression Value)
        : Statement;

        internal sealed record DoWhileLoop(SemanticMetadata Meta,
            Expression Condition,
            SemanticBlock Block)
        : Statement;

        internal interface Builtin : Statement
        {
            internal sealed record Ecrire(SemanticMetadata Meta,
                Expression ArgumentNomLog,
                Expression ArgumentExpression)
            : Builtin;

            internal sealed record Fermer(SemanticMetadata Meta,
                Expression ArgumentNomLog)
            : Builtin;

            internal sealed record Lire(SemanticMetadata Meta,
                Expression ArgumentNomLog,
                Expression.Lvalue ArgumentVariable)
            : Builtin;

            internal sealed record OuvrirAjout(SemanticMetadata Meta,
                Expression ArgumentNomLog)
            : Builtin;

            internal sealed record OuvrirEcriture(SemanticMetadata Meta,
                Expression ArgumentNomLog)
            : Builtin;

            internal sealed record OuvrirLecture(SemanticMetadata Meta,
                Expression ArgumentNomLog)
            : Builtin;

            internal sealed record Assigner(SemanticMetadata Meta,
                Expression.Lvalue ArgumentNomLog,
                Expression ArgumentNomExt)
            : Builtin;

            internal sealed record EcrireEcran(SemanticMetadata Meta,
                IReadOnlyList<Expression> Arguments)
            : Builtin;

            internal sealed record LireClavier(SemanticMetadata Meta,
                Expression.Lvalue ArgumentVariable)
            : Builtin;
        }

        internal sealed record ForLoop(SemanticMetadata Meta,
            Expression.Lvalue Variant,
            Expression Start,
            Expression End,
            Option<Expression> Step,
            SemanticBlock Block)
        : Statement;

        internal sealed record ProcedureCall(SemanticMetadata Meta,
            Identifier Name,
            IReadOnlyList<ParameterActual> Parameters)
        : Statement, SemanticCallNode;

        internal sealed record RepeatLoop(SemanticMetadata Meta,
            Expression Condition,
            SemanticBlock Block)
        : Statement;

        internal sealed record Return(SemanticMetadata Meta,
            Expression Value)
        : Statement;

        internal sealed record LocalVariable(SemanticMetadata Meta,
            VariableDeclaration Declaration,
            Option<Initializer> Initializer)
        : Statement;

        internal sealed record WhileLoop(SemanticMetadata Meta,
            Expression Condition,
            SemanticBlock Block) : Statement;
    }

    internal interface Initializer : SemanticNode
    {
        public Value Value { get; }

        internal sealed record Braced(SemanticMetadata Meta,
            IReadOnlyList<Braced.Item> Items,
            Value Value)
        : Initializer
        {
            internal sealed record Item(SemanticMetadata Meta,
                Option<Designator> Designator,
                Initializer Initializer);
        }
    }

    internal interface Expression : Initializer
    {
        internal interface Lvalue : Expression
        {
            internal sealed record ComponentAccess(SemanticMetadata Meta,
                Expression Structure,
                Identifier ComponentName,
                Value Value)
            : Lvalue;

            internal new sealed record Bracketed(SemanticMetadata Meta,
                Lvalue ContainedLValue,
                Value Value)
            : Lvalue, SemanticBracketedExpressionNode
            {
                Expression SemanticBracketedExpressionNode.ContainedExpression => ContainedLValue;
            }

            internal sealed record ArraySubscript(SemanticMetadata Meta,
                Expression Array,
                IReadOnlyList<Expression> Index,
                Value Value)
            : Lvalue;

            internal sealed record VariableReference(SemanticMetadata Meta,
                Identifier Name,
                Value Value)
            : Lvalue;
        }

        internal sealed record UnaryOperation(SemanticMetadata Meta,
            UnaryOperator Operator,
            Expression Operand,
            Value Value)
        : Expression;

        internal sealed record BinaryOperation(SemanticMetadata Meta,
            Expression Left,
            BinaryOperator Operator,
            Expression Right,
            Value Value)
        : Expression;

        internal sealed record BuiltinFdf(SemanticMetadata Meta,
            Expression ArgumentNomLog,
            Value Value)
        : Expression;

        internal sealed record FunctionCall(SemanticMetadata Meta,
            Identifier Name,
            IReadOnlyList<ParameterActual> Parameters,
            Value Value)
        : Expression, SemanticCallNode;

        internal sealed record Bracketed(SemanticMetadata Meta,
            Expression ContainedExpression,
            Value Value)
        : Expression, SemanticBracketedExpressionNode;

        internal sealed record Literal(SemanticMetadata Meta,
            IConvertible UnderlyingValue,
            Value Value)
        : Expression;
    }

    internal interface Designator : SemanticNode
    {
        internal sealed record Array(SemanticMetadata Meta,
            IReadOnlyList<Expression> Index)
        : Designator;

        internal sealed record Structure(SemanticMetadata Meta,
            Identifier Component)
        : Designator;
    }

    internal sealed record ParameterActual(SemanticMetadata Meta,
        ParameterMode Mode,
        Expression Value)
    : SemanticNode;

    internal sealed record ParameterFormal(SemanticMetadata Meta,
        ParameterMode Mode,
        Identifier Name,
        EvaluatedType Type)
    : SemanticNode;

    internal sealed record VariableDeclaration(SemanticMetadata Meta,
                IReadOnlyList<Identifier> Names,
                EvaluatedType Type) : SemanticNode;

    internal sealed record ProcedureSignature(SemanticMetadata Meta,
        Identifier Name,
        IReadOnlyList<ParameterFormal> Parameters)
    : SemanticNode;

    internal sealed record FunctionSignature(SemanticMetadata Meta,
        Identifier Name,
        IReadOnlyList<ParameterFormal> Parameters,
        EvaluatedType ReturnType)
    : SemanticNode;

    internal interface UnaryOperator : SemanticNode
    {
        public sealed record Cast(SemanticMetadata Meta,
            EvaluatedType Target)
        : UnaryOperator;

        public sealed record Minus(SemanticMetadata Meta) : UnaryOperator;
        public sealed record Not(SemanticMetadata Meta) : UnaryOperator;
        public sealed record Plus(SemanticMetadata Meta) : UnaryOperator;
    }

    internal interface BinaryOperator : SemanticNode
    {
        public sealed record Add(SemanticMetadata Meta) : BinaryOperator;
        public sealed record And(SemanticMetadata Meta) : BinaryOperator;
        public sealed record Divide(SemanticMetadata Meta) : BinaryOperator;
        public sealed record Equal(SemanticMetadata Meta) : BinaryOperator;
        public sealed record GreaterThan(SemanticMetadata Meta) : BinaryOperator;
        public sealed record GreaterThanOrEqual(SemanticMetadata Meta) : BinaryOperator;
        public sealed record LessThan(SemanticMetadata Meta) : BinaryOperator;
        public sealed record LessThanOrEqual(SemanticMetadata Meta) : BinaryOperator;
        public sealed record Mod(SemanticMetadata Meta) : BinaryOperator;
        public sealed record Multiply(SemanticMetadata Meta) : BinaryOperator;
        public sealed record Or(SemanticMetadata Meta) : BinaryOperator;
        public sealed record NotEqual(SemanticMetadata Meta) : BinaryOperator;
        public sealed record Xor(SemanticMetadata Meta) : BinaryOperator;
        public sealed record Subtract(SemanticMetadata Meta) : BinaryOperator;
    }
}
