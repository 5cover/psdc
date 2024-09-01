using static Scover.Psdc.StaticAnalysis.SemanticNode;

namespace Scover.Psdc.Pseudocode;

static class ComptimeExpression
{
    public static ComptimeExpression<TUnderlying> Create<TUnderlying>(Expression expression, TUnderlying value)
     => new(expression, value);
}

readonly record struct ComptimeExpression<TUnderlying>(Expression Expression, TUnderlying Value)
{
    public override string? ToString() => Value?.ToString();
}
