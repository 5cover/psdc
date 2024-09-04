using Scover.Psdc.Pseudocode;
using static Scover.Psdc.StaticAnalysis.SemanticNode;

namespace Scover.Psdc.StaticAnalysis;

public static class SemanticNodeExtensions
{
    static SemanticMetadata Meta(SemanticNode node) => new(node.Meta.Scope, SourceTokens.Empty);

    /// Invert an expression, assuming it is boolean.
    /// </summary>
    /// <param name="expr">The expression.</param>
    /// <returns>The inverse of <paramref name="expr"/>. If <param name="expr"> is not a boolean expression, a simple NOT logical operation is created.</returns>
    internal static Expression Invert(this Expression expr) => expr switch {
        Expression.Literal { Value: BooleanValue v } l => new Expression.Literal(Meta(expr),
            v.Status.ComptimeValue.Match(v => !v, () => l.UnderlyingValue), v.Invert()),
        Expression.Bracketed e => new Expression.Bracketed(Meta(expr), e.ContainedExpression.Invert(), e.Value.Invert()),
        Expression.UnaryOperation uo when uo.Operator is UnaryOperator.Not => uo.Operand,
        Expression.BinaryOperation bo when bo.Invert() is { HasValue: true } bov => bov.Value,
        _ => new Expression.UnaryOperation(Meta(expr), new UnaryOperator.Not(Meta(expr)), expr, expr.Value.Invert()),
    };

    static Value Invert(this Value val)
     => val is BooleanValue b ?
        b.Map(b => !b)
        : UnknownType.Inferred.DefaultValue;

    static ValueOption<Expression.BinaryOperation> Invert(this Expression.BinaryOperation bo)
    {
        var v = bo.Value.Invert();
        return bo.Operator switch {
            BinaryOperator.Equal e => new Expression.BinaryOperation(Meta(bo), bo.Left, new BinaryOperator.NotEqual(Meta(e)), bo.Right, v),
            BinaryOperator.NotEqual e => new Expression.BinaryOperation(Meta(bo), bo.Left, new BinaryOperator.Equal(Meta(e)), bo.Right, v),
            BinaryOperator.GreaterThan e => new Expression.BinaryOperation(Meta(bo), bo.Left, new BinaryOperator.LessThanOrEqual(Meta(e)), bo.Right, v),
            BinaryOperator.GreaterThanOrEqual e => new Expression.BinaryOperation(Meta(bo), bo.Left, new BinaryOperator.LessThan(Meta(e)), bo.Right, v),
            BinaryOperator.LessThan e => new Expression.BinaryOperation(Meta(bo), bo.Left, new BinaryOperator.GreaterThanOrEqual(Meta(e)), bo.Right, v),
            BinaryOperator.LessThanOrEqual e => new Expression.BinaryOperation(Meta(bo), bo.Left, new BinaryOperator.GreaterThan(Meta(e)), bo.Right, v),
            // NOT (A AND B) <=> NOT A OR NOT B
            BinaryOperator.And e => new Expression.BinaryOperation(Meta(bo),
                bo.Left.Invert(),
                new BinaryOperator.Or(Meta(e)),
                bo.Right.Invert(),
                v),
            // NOT (A OR B) <=> NOT A AND NOT B
            BinaryOperator.Or e => new Expression.BinaryOperation(Meta(bo),
                bo.Left.Invert(),
                new BinaryOperator.And(Meta(e)),
                bo.Right.Invert(),
                v),
            // NOT (A XOR B) <=> A = B
            BinaryOperator.Xor e => new Expression.BinaryOperation(Meta(bo),
                bo.Left,
                new BinaryOperator.Equal(Meta(e)),
                bo.Right,
                v).Some(), // gives the target type of the switch expression
            _ => default
        };
    }

    internal static ValueOption<Expression.Literal> ToLiteral<TType, TUnderlying>(this Value<TType, TUnderlying> value, Scope scope)
    where TType : EvaluatedType
    where TUnderlying : notnull
     => value.Status is ValueStatus.Comptime<TUnderlying> comptime
         ? new Expression.Literal(new(scope, SourceTokens.Empty), comptime.Value, value).Some()
         : default;
}
