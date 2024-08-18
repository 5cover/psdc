namespace Scover.Psdc.Language;

abstract class ValueImpl<TType>(TType type, ValueStatus status) : Value
where TType : EvaluatedType
{
    public TType Type { get; } = type;
    public ValueStatus Status { get; } = status;
    EvaluatedType Value.Type => Type;

    public bool Equals(Value? other) => other is ValueImpl<TType> o
     && o.Type.SemanticsEqual(Type)
     && o.Status.Equals(Status);
}

abstract class ValueImpl<TSelf, TType, TUnderlying>(TType type, ValueStatus<TUnderlying> status) : Value<TType, TUnderlying>
where TType : EvaluatedType
where TSelf : ValueImpl<TSelf, TType, TUnderlying>
where TUnderlying : notnull
{
    public TType Type { get; } = type;
    public ValueStatus<TUnderlying> Status { get; } = status;
    EvaluatedType Value.Type => Type;
    ValueStatus Value.Status => Status;
    public bool Equals(Value? other) => other is TSelf o
     && o.Type.SemanticsEqual(Type)
     && o.Status.Equals(Status);
    public TSelf Map(Func<TUnderlying, TUnderlying> transform) => Clone(Status.Map(transform));

    protected abstract TSelf Clone(ValueStatus<TUnderlying> value);

}
