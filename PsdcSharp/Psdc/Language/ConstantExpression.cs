using static Scover.Psdc.StaticAnalysis.SemanticNode;

namespace Scover.Psdc.Language;

static class ComptimeExpression
{
    public static ComptimeExpression<TValue> Create<TValue>(Expression expression, TValue value)
     => new(expression, value);
}

sealed record ComptimeExpression<TValue>(Expression Expression, TValue Value)
{
    public override string? ToString() => Value?.ToString();
}
