using Scover.Psdc.Pseudocode;

using static Scover.Psdc.StaticAnalysis.SemanticNode;

namespace Scover.Psdc.StaticAnalysis;

public static class SemanticNodeExtensions
{
    /// <summary>
    /// Invert an expression, assuming it is boolean.
    /// </summary>
    /// <param name="expr">The expression.</param>
    /// <returns>The inverse of <paramref name="expr"/>. If <param name="expr"/> is not a boolean expression, a simple NOT logical operation is created.</returns>
    internal static Expr Invert(this Expr expr) => expr switch {
        Expr.Literal { Value: BooleanValue v } l => new Expr.Literal(expr.Meta,
            v.Status.ComptimeValue.Match(v => !v, () => l.UnderlyingValue), v.Invert()),
        Expr.ParenExprImpl e => new Expr.ParenExprImpl(expr.Meta, e.ContainedExpression.Invert(), e.Value.Invert()),
        Expr.UnaryOperation { Operator: UnaryOperator.Not } uo => uo.Operand,
        Expr.BinaryOperation bo when bo.Invert() is { HasValue: true } bov => bov.Value,
        _ => new Expr.UnaryOperation(expr.Meta, new UnaryOperator.Not(expr.Meta), expr, expr.Value.Invert()),
    };

    static Value Invert(this Value val) => val is BooleanValue b
        ? b.Map(b => !b)
        : UnknownType.Inferred.DefaultValue;

    static ValueOption<Expr.BinaryOperation> Invert(this Expr.BinaryOperation bo)
    {
        var v = bo.Value.Invert();
        return bo.Operator switch {
            BinaryOperator.Equal e => bo with { Operator = new BinaryOperator.NotEqual(e.Meta), Value = v },
            BinaryOperator.NotEqual e => bo with { Operator = new BinaryOperator.Equal(e.Meta), Value = v },
            BinaryOperator.GreaterThan e => bo with { Operator = new BinaryOperator.LessThanOrEqual(e.Meta), Value = v },
            BinaryOperator.GreaterThanOrEqual e => bo with { Operator = new BinaryOperator.LessThan(e.Meta), Value = v },
            BinaryOperator.LessThan e => bo with { Operator = new BinaryOperator.GreaterThanOrEqual(e.Meta), Value = v },
            BinaryOperator.LessThanOrEqual e => bo with { Operator = new BinaryOperator.GreaterThan(e.Meta), Value = v },
            // NOT (A AND B) <=> NOT A OR NOT B
            BinaryOperator.And e => new Expr.BinaryOperation(bo.Meta,
                bo.Left.Invert(),
                new BinaryOperator.Or(e.Meta),
                bo.Right.Invert(),
                v),
            // NOT (A OR B) <=> NOT A AND NOT B
            BinaryOperator.Or e => new Expr.BinaryOperation(bo.Meta,
                bo.Left.Invert(),
                new BinaryOperator.And(e.Meta),
                bo.Right.Invert(),
                v),
            // NOT (A XOR B) <=> A = B
            BinaryOperator.Xor e =>
                (bo with { Operator = new BinaryOperator.Equal(e.Meta), Value = v }).Some(), // gives the target type of the switch expression
            _ => default,
        };
    }

    internal static ValueOption<Expr.Literal> ToLiteral<TType, TUnderlying>(this Value<TType, TUnderlying> value, Scope scope)
    where TType : EvaluatedType
    where TUnderlying : notnull => value.Status is ValueStatus.Comptime<TUnderlying> comptime
        ? new Expr.Literal(new(scope, default), comptime.Value, value).Some()
        : default;
}
