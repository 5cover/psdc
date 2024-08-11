using static Scover.Psdc.StaticAnalysis.SemanticNode;

namespace Scover.Psdc.Language;

static class ConstantExpression
{
    public static ConstantExpression<TValue> Create<TValue>(Expression expression, TValue value)
     => new(expression, value);
}

sealed record ConstantExpression<TValue>(Expression Expression, TValue Value)
{
    public override string? ToString() => Value?.ToString();
}
