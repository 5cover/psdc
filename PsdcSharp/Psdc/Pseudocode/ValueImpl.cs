using Scover.Psdc.CodeGeneration;

namespace Scover.Psdc.Pseudocode;

file static class ValueStatusExtensions
{
    public static string GetPart(this ValueStatus valueStatus) => valueStatus switch {
        ValueStatus.Comptime => "comptime ",
        ValueStatus.Garbage => "garbage ",
        ValueStatus.Invalid => "invalid ",
        ValueStatus.Runtime => "runtime ",
        _ => throw valueStatus.ToUnmatchedException(),
    };
}

abstract class ValueImpl<TType>(TType type, ValueStatus status) : FormattableUsableImpl, Value
where TType : EvaluatedType
{
    public TType Type { get; } = type;
    public ValueStatus Status { get; } = status;
    EvaluatedType Value.Type => Type;
    public bool Equals(Value? other) => other is ValueImpl<TType> o
     && o.Type.SemanticsEqual(Type)
     && o.Status.Equals(Status);
    public override string ToString(string? format, IFormatProvider? fmtProvider) => format switch {
        Value.FmtMin => Type.ToString(fmtProvider),
        _ when string.IsNullOrEmpty(format) => string.Create(fmtProvider, $"{Status.GetPart()}'{Type}'"),
        _ => throw new FormatException($"Unsupported format: '{format}'"),
    };
    public string ToString(string? format, IFormatProvider? fmtProvider, Indentation indent) => ToString(format, fmtProvider);
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
    protected abstract string ValueToString(TUnderlying value, IFormatProvider? fmtProvider, Indentation indent);
    public override string ToString(string? format, IFormatProvider? fmtProvider) => ToString(format, fmtProvider, new(2));
    public string ToString(string? format, IFormatProvider? fmtProvider, Indentation indent) => format switch {
        Value.FmtNoType => Status.ComptimeValue.Map(v => ValueToString(v, fmtProvider, indent)).ValueOr(""),
        Value.FmtMin => Status.ComptimeValue.Map(v => ValueToString(v, fmtProvider, indent)).ValueOr(Type.ToString(fmtProvider)),
        "" or null => Status.ComptimeValue.Match(
             v => string.Create(fmtProvider, $"{Status.GetPart()}'{Type}' : {ValueToString(v, fmtProvider, indent)}"),
            () => string.Create(fmtProvider, $"{Status.GetPart()}'{Type}'")),
        _ => throw new FormatException($"Unsupported format: '{format}'"),
    };
}
