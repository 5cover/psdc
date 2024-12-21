using static Scover.Psdc.StaticAnalysis.SemanticNode;

namespace Scover.Psdc.Pseudocode;

static class ComptimeExpression
{
    public static ComptimeExpression<TUnderlying> Create<TUnderlying>(Expr expression, TUnderlying value)
     => new(expression, value);
}

readonly record struct ComptimeExpression<TUnderlying>(Expr Expression, TUnderlying Value)
{
    public override string? ToString() => Value?.ToString();
}
