namespace Scover.Psdc.Language;

abstract class ValueImpl<TType>(TType type, ValueStatus status) : FormattableUsableImpl, Value
where TType : EvaluatedType
{
    public TType Type { get; } = type;
    public ValueStatus Status { get; } = status;
    EvaluatedType Value.Type => Type;
    public bool Equals(Value? other) => other is ValueImpl<TType> o
     && o.Type.SemanticsEqual(Type)
     && o.Status.Equals(Status);
    public override string ToString(string? format, IFormatProvider? formatProvider) => format switch {
        Value.FmtMin => Type.ToString(formatProvider),
        _ when string.IsNullOrEmpty(format) => string.Create(formatProvider, $"{Status.Representation} {Type}"),
        _ => throw new FormatException($"Unsupported format: '{format}'")
    };
}

abstract class ValueImpl<TSelf, TType, TUnderlying>(TType type, ValueStatus<TUnderlying> status) : FormattableUsableImpl, Value<TType, TUnderlying>
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
    protected abstract string ValueToString(TUnderlying value, IFormatProvider? formatProvider);
    public override string ToString(string? format, IFormatProvider? fmtProvider) => format switch {
        Value.FmtMin => Status.ComptimeValue.Map(v => ValueToString(v, fmtProvider)).ValueOr(Type.ToString(fmtProvider)),
        _ when string.IsNullOrEmpty(format) => Status.ComptimeValue.Match(
             v => string.Create(fmtProvider, $"{Status.Representation} {Type}: {ValueToString(v, fmtProvider)}"),
            () => string.Create(fmtProvider, $"{Status.Representation} {Type}")),
        _ => throw new FormatException($"Unsupported format: '{format}'")
    };
}
