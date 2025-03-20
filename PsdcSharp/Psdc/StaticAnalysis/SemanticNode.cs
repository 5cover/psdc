global using SemanticBlock = System.Collections.Generic.IReadOnlyList<Scover.Psdc.StaticAnalysis.SemanticNode.Statement>;

using Scover.Psdc.Pseudocode;
using Scover.Psdc.Parsing;

namespace Scover.Psdc.StaticAnalysis;

public readonly record struct SemanticMetadata(Scope Scope, Range Location);
public interface SemanticNode
{
    SemanticMetadata Meta { get; }

    internal interface ParenExpr : Expr
    {
        Expr ContainedExpression { get; }
    }
    sealed record Algorithm(SemanticMetadata Meta, Ident Title, IReadOnlyList<Declaration> Declarations) : SemanticNode;
    interface Declaration : SemanticNode
    {
        internal sealed record MainProgram(SemanticMetadata Meta, SemanticBlock Block) : Declaration;

        internal sealed record TypeAlias(SemanticMetadata Meta, Ident Name, EvaluatedType Type) : Declaration;

        internal sealed record Constant(SemanticMetadata Meta, EvaluatedType Type, Ident Name, Initializer Value) : Declaration;

        internal sealed record Callable(SemanticMetadata Meta, CallableSignature Signature) : Declaration;

        internal sealed record CallableDefinition(SemanticMetadata Meta, CallableSignature Signature, SemanticBlock Block) : Declaration;
    }

    internal interface Statement : SemanticNode
    {
        internal sealed record ExpressionStatement(SemanticMetadata Meta, Expr Expression) : Statement;

        internal sealed record Alternative(
            SemanticMetadata Meta,
            Alternative.IfClause If,
            IReadOnlyList<Alternative.ElseIfClause> ElseIfs,
            Option<Alternative.ElseClause> Else
        ) : Statement
        {
            internal sealed record IfClause(SemanticMetadata Meta, Expr Condition, SemanticBlock Block) : SemanticNode;

            internal sealed record ElseIfClause(SemanticMetadata Meta, Expr Condition, SemanticBlock Block) : SemanticNode;

            internal sealed record ElseClause(SemanticMetadata Meta, SemanticBlock Block) : SemanticNode;
        }

        internal sealed record Switch(SemanticMetadata Meta, Expr Expression, IReadOnlyList<Switch.Case> Cases) : Statement
        {
            internal interface Case : SemanticNode
            {
                SemanticBlock Block { get; }

                internal sealed record OfValue(SemanticMetadata Meta, Expr Value, SemanticBlock Block) : Case;

                internal sealed record Default(SemanticMetadata Meta, SemanticBlock Block) : Case;
            }
        }

        internal sealed record Assignment(SemanticMetadata Meta, Expr.Lvalue Target, Expr Value) : Statement;

        internal sealed record DoWhileLoop(SemanticMetadata Meta, Expr Condition, SemanticBlock Block) : Statement;

        internal interface Builtin : Statement
        {
            internal sealed record Ecrire(SemanticMetadata Meta, Expr ArgumentNomLog, Expr ArgumentExpression) : Builtin;

            internal sealed record Fermer(SemanticMetadata Meta, Expr ArgumentNomLog) : Builtin;

            internal sealed record Lire(SemanticMetadata Meta, Expr ArgumentNomLog, Expr.Lvalue ArgumentVariable) : Builtin;

            internal sealed record OuvrirAjout(SemanticMetadata Meta, Expr ArgumentNomLog) : Builtin;

            internal sealed record OuvrirEcriture(SemanticMetadata Meta, Expr ArgumentNomLog) : Builtin;

            internal sealed record OuvrirLecture(SemanticMetadata Meta, Expr ArgumentNomLog) : Builtin;

            internal sealed record Assigner(SemanticMetadata Meta, Expr.Lvalue ArgumentNomLog, Expr ArgumentNomExt) : Builtin;

            internal sealed record EcrireEcran(SemanticMetadata Meta, IReadOnlyList<Expr> Arguments) : Builtin;

            internal sealed record LireClavier(SemanticMetadata Meta, Expr.Lvalue ArgumentVariable) : Builtin;
        }

        internal sealed record ForLoop(SemanticMetadata Meta, Expr.Lvalue Variant, Expr Start, Expr End, Option<Expr> Step, SemanticBlock Block) : Statement;

        internal sealed record RepeatLoop(SemanticMetadata Meta, Expr Condition, SemanticBlock Block) : Statement;

        internal sealed record Return(SemanticMetadata Meta, Option<Expr> Value) : Statement;

        internal sealed record LocalVariable(SemanticMetadata Meta, VariableDeclaration Decl, Option<Initializer> Value) : Statement;

        internal sealed record WhileLoop(SemanticMetadata Meta, Expr Condition, SemanticBlock Block) : Statement;
    }

    internal interface Initializer : SemanticNode
    {
        Value Value { get; }

        internal sealed record Braced(SemanticMetadata Meta, IReadOnlyList<Braced.Item> Items, Value Value) : Initializer
        {
            internal sealed record Item(SemanticMetadata Meta, IReadOnlyList<Designator> Designators, Initializer Value) : SemanticNode;
        }
    }

    internal interface Expr : Initializer
    {
        internal interface Lvalue : Expr
        {
            internal sealed record ComponentAccess(SemanticMetadata Meta, Expr Structure, Ident ComponentName, Value Value) : Lvalue;

            internal sealed record ParenLValue(SemanticMetadata Meta, Lvalue ContainedLValue, Value Value) : Lvalue, ParenExpr
            {
                Expr ParenExpr.ContainedExpression => ContainedLValue;
            }

            internal sealed record ArraySubscript(SemanticMetadata Meta, Expr Array, Expr Index, Value Value) : Lvalue;

            internal sealed record VariableReference(SemanticMetadata Meta, Ident Name, Value Value) : Lvalue;
        }

        internal sealed record UnaryOperation(SemanticMetadata Meta, UnaryOperator Operator, Expr Operand, Value Value) : Expr;

        internal sealed record BinaryOperation(SemanticMetadata Meta, Expr Left, BinaryOperator Operator, Expr Right, Value Value) : Expr;

        internal sealed record BuiltinFdf(SemanticMetadata Meta, Expr ArgumentNomLog, Value Value) : Expr;

        internal sealed record Call(SemanticMetadata Meta, Ident Callee, IReadOnlyList<ParameterActual> Parameters, Value Value) : Expr;

        internal sealed record ParenExprImpl(SemanticMetadata Meta, Expr ContainedExpression, Value Value) : ParenExpr;

        internal sealed record Literal(SemanticMetadata Meta, object UnderlyingValue, Value Value) : Expr;
    }

    internal interface Designator : SemanticNode
    {
        internal sealed record Array(SemanticMetadata Meta, ComptimeExpression<int> Index) : Designator;

        internal sealed record Structure(SemanticMetadata Meta, Ident Component) : Designator;
    }

    internal sealed record ParameterActual(SemanticMetadata Meta, ParameterMode Mode, Expr Value) : SemanticNode;

    internal sealed record ParameterFormal(SemanticMetadata Meta, ParameterMode Mode, Ident Name, EvaluatedType Type) : SemanticNode;

    internal sealed record VariableDeclaration(SemanticMetadata Meta, IReadOnlyList<Ident> Names, EvaluatedType Type) : SemanticNode;

    internal sealed record CallableSignature(SemanticMetadata Meta, Ident Name, IReadOnlyList<ParameterFormal> Parameters, EvaluatedType ReturnType)
        : SemanticNode;

    internal interface UnaryOperator : SemanticNode
    {
        sealed record Cast(SemanticMetadata Meta, EvaluatedType Target) : UnaryOperator;
        sealed record Minus(SemanticMetadata Meta) : UnaryOperator;
        sealed record Not(SemanticMetadata Meta) : UnaryOperator;
        sealed record Plus(SemanticMetadata Meta) : UnaryOperator;
    }

    internal interface BinaryOperator : SemanticNode
    {
        sealed record Add(SemanticMetadata Meta) : BinaryOperator;
        sealed record And(SemanticMetadata Meta) : BinaryOperator;
        sealed record Divide(SemanticMetadata Meta) : BinaryOperator;
        sealed record Equal(SemanticMetadata Meta) : BinaryOperator;
        sealed record GreaterThan(SemanticMetadata Meta) : BinaryOperator;
        sealed record GreaterThanOrEqual(SemanticMetadata Meta) : BinaryOperator;
        sealed record LessThan(SemanticMetadata Meta) : BinaryOperator;
        sealed record LessThanOrEqual(SemanticMetadata Meta) : BinaryOperator;
        sealed record Mod(SemanticMetadata Meta) : BinaryOperator;
        sealed record Multiply(SemanticMetadata Meta) : BinaryOperator;
        sealed record Or(SemanticMetadata Meta) : BinaryOperator;
        sealed record NotEqual(SemanticMetadata Meta) : BinaryOperator;
        sealed record Xor(SemanticMetadata Meta) : BinaryOperator;
        sealed record Subtract(SemanticMetadata Meta) : BinaryOperator;
    }

    internal sealed record Nop(SemanticMetadata Meta) : Statement, Declaration;
}
