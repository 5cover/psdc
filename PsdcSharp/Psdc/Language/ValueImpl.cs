namespace Scover.Psdc.Language;

abstract class ValueImpl<TType>(TType type, ValueStatus value) : Value
where TType : EvaluatedType
{
    public TType Type { get; } = type;
    public ValueStatus Value { get; } = value;
    EvaluatedType Value.Type => Type;

    public bool SemanticsEqual(Value other) => other is ValueImpl<TType> o
     && o.Type.SemanticsEqual(Type)
     && o.Value.Equals(Value);
}

abstract class ValueImpl<TType, TUnderlying>(TType type, ValueStatus<TUnderlying> value) : Value<TType, TUnderlying>
where TType : EvaluatedType
{
    public TType Type { get; } = type;
    public ValueStatus<TUnderlying> Value { get; } = value;
    EvaluatedType Value.Type => Type;

    ValueStatus Value.Value => Value.Value;

    public bool SemanticsEqual(Value other) => other is ValueImpl<TType, TUnderlying> o
     && o.Type.SemanticsEqual(Type)
     && o.Value.SemanticsEqual(Value);
}
