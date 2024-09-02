using System.Collections.Immutable;
using System.Text;
using Scover.Psdc.Parsing;

namespace Scover.Psdc.Pseudocode;

interface Value : IEquatable<Value>, IFormattableUsable
{
    EvaluatedType Type { get; }
    ValueStatus Status { get; }
    /// <summary>
    /// Format string for a minimal representation.
    /// </summary>
    public const string FmtMin = "m";
    /// <summary>
    /// Format string for only the comptime value - no type. Returns the empty string if the value doesn't have a comptime value.
    /// </summary>
    public const string FmtNoType = "v";
}

interface Value<out TType, TUnderlying> : Value where TType : EvaluatedType
{
    new ValueStatus<TUnderlying> Status { get; }
    new TType Type { get; }
}

sealed class IntegerValue(IntegerType type, ValueStatus<int> value)
: ValueImpl<IntegerValue, IntegerType, int>(type, value), RealValue
{
    RealType Value<RealType, decimal>.Type => RealType.Instance;
    ValueStatus<decimal> Value<RealType, decimal>.Status => Status.Map(v => (decimal)v);
    protected override IntegerValue Clone(ValueStatus<int> value) => new(Type, value);
    protected override string ValueToString(int value, IFormatProvider? fmtProvider) => value.ToString(fmtProvider);
}

interface RealValue : Value<RealType, decimal>;

sealed class RealValueImpl(RealType type, ValueStatus<decimal> value) : ValueImpl<RealValueImpl, RealType, decimal>(type, value), RealValue
{
    protected override RealValueImpl Clone(ValueStatus<decimal> value) => new(Type, value);
    protected override string ValueToString(decimal value, IFormatProvider? fmtProvider) => value.ToString(fmtProvider);
}

interface StringValue : Value<StringType, string>;

sealed class StringValueImpl(StringType type, ValueStatus<string> value) : ValueImpl<StringValueImpl, StringType, string>(type, value), StringValue
{
    protected override StringValueImpl Clone(ValueStatus<string> value) => new(Type, value);
    protected override string ValueToString(string value, IFormatProvider? fmtProvider)
     => $"\"{Strings.Escape(value, EscapeMode.ForString, fmtProvider)}\"";
}

sealed class StructureValue(StructureType type, ValueStatus<ImmutableDictionary<Identifier, Value>> value) : ValueImpl<StructureValue, StructureType, ImmutableDictionary<Identifier, Value>>(type, value)
{
    protected override StructureValue Clone(ValueStatus<ImmutableDictionary<Identifier, Value>> value) => new(Type, value);
    protected override string ValueToString(ImmutableDictionary<Identifier, Value> value, IFormatProvider? fmtProvider)
     => new StringBuilder("{ ")
        .AppendJoin(", ", value.Select(kvp => $".{kvp.Key} := {kvp.Value.ToString(Value.FmtMin, fmtProvider)}"))
        .Append(" }")
        .ToString();
}

sealed class VoidValue(VoidType type, ValueStatus value) : ValueImpl<VoidType>(type, value);

sealed class UnknownValue(UnknownType type, ValueStatus value) : ValueImpl<UnknownType>(type, value);

sealed class ArrayValue(ArrayType type, ValueStatus<ImmutableArray<Value>> value) : ValueImpl<ArrayValue, ArrayType, ImmutableArray<Value>>(type, value)
{
    protected override ArrayValue Clone(ValueStatus<ImmutableArray<Value>> value) => new(Type, value);
    protected override string ValueToString(ImmutableArray<Value> value, IFormatProvider? fmtProvider)
     => new StringBuilder("{ ")
        .AppendJoin(", ", value.Select(v => v.ToString(Value.FmtMin, fmtProvider)))
        .Append(" }")
        .ToString();
}

sealed class BooleanValue(BooleanType type, ValueStatus<bool> value) : ValueImpl<BooleanValue, BooleanType, bool>(type, value)
{
    protected override BooleanValue Clone(ValueStatus<bool> value) => new(Type, value);
    protected override string ValueToString(bool value, IFormatProvider? fmtProvider) => value ? "vrai" : "faux";
}

sealed class CharacterValue(CharacterType type, ValueStatus<char> value) : ValueImpl<CharacterValue, CharacterType, char>(type, value)
{
    protected override CharacterValue Clone(ValueStatus<char> value) => new(Type, value);
    protected override string ValueToString(char value, IFormatProvider? fmtProvider)
     => $"'{Strings.Escape(value.ToString(fmtProvider), EscapeMode.ForChar, fmtProvider)}'";
}

sealed class FileValue(FileType type, ValueStatus value) : ValueImpl<FileType>(type, value);

sealed class LengthedStringValue(LengthedStringType type, ValueStatus<string> value)
: ValueImpl<LengthedStringValue, LengthedStringType, string>(type, value), StringValue
{
    StringType Value<StringType, string>.Type => StringType.Instance;

    protected override LengthedStringValue Clone(ValueStatus<string> value) => new(Type, value);
    protected override string ValueToString(string value, IFormatProvider? fmtProvider)
     => $"\"{Strings.Escape(value, EscapeMode.ForString, fmtProvider)}\"";
}