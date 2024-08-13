namespace Scover.Psdc.Language;

abstract class ValueImpl<TType>(TType type, ValueStatus value) : Value
where TType : EvaluatedType
{
    public TType Type { get; } = type;
    public ValueStatus Status { get; } = value;
    EvaluatedType Value.Type => Type;

    public bool SemanticsEqual(Value other) => other is ValueImpl<TType> o
     && o.Type.SemanticsEqual(Type)
     && o.Status.Equals(Status);
}

abstract class ValueImpl<TSelf, TType, TUnderlying>(TType type, ValueStatus<TUnderlying> value) : Value<TType, TUnderlying>
where TType : EvaluatedType
where TSelf : ValueImpl<TSelf, TType, TUnderlying>
{
    public TType Type { get; } = type;
    public ValueStatus<TUnderlying> Status { get; } = value;
    EvaluatedType Value.Type => Type;

    ValueStatus Value.Status => Status.Status;

    public bool SemanticsEqual(Value other) => other is ValueImpl<TSelf, TType, TUnderlying> o
     && o.Type.SemanticsEqual(Type)
     && o.Status.SemanticsEqual(Status);
     
    public TSelf Map(Func<TUnderlying, TUnderlying> transform) => Clone(Status.Map(transform));

    protected abstract TSelf Clone(ValueStatus<TUnderlying> value);
    
}
