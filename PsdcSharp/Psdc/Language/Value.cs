namespace Scover.Psdc.Language;

interface Value : EquatableSemantics<Value>
{
    EvaluatedType Type { get; }
    bool IsKnown { get; }
}

interface Value<TUnderlying>
{
    public Option<TUnderlying> UnderlyingValue { get; }
}

interface Value<TType, TUnderlying> : Value<TUnderlying>, Value, EquatableSemantics<Value<TType, TUnderlying>>
where TType : EvaluatedType
{
    new TType Type { get; }

}

abstract class ValueImpl<TType, TUnderlying>(TType type, Option<TUnderlying> underlyingValue) : Value<TType, TUnderlying>
where TType : EvaluatedType
{
    public TType Type => type;
    public Option<TUnderlying> UnderlyingValue => underlyingValue;
    public bool IsKnown => UnderlyingValue.HasValue;
    EvaluatedType Value.Type => type;

    public bool Equals(Value<TUnderlying>? other) => other is { } o
     && EqualityComparer<Option<TUnderlying>>.Default.Equals(o.UnderlyingValue, UnderlyingValue);

    public bool SemanticsEqual(Value<TType, TUnderlying> other)
     => other.Type.SemanticsEqual(Type)
     && other.UnderlyingValue.Equals(UnderlyingValue);
    public bool SemanticsEqual(Value other) => other.Type.SemanticsEqual(Type);
}
