using Scover.Psdc.Parsing;

namespace Scover.Psdc.Language;

interface Value : EquatableSemantics<Value>
{
    EvaluatedType Type { get; }
    ValueStatus Status { get; }

    /// <summary>
    /// Status for a value that is known at compile-time.
    /// </summary>
    /// <typeparam name="TUnderlying">The type of the underlying value.</typeparam>
    /// <param name="value">The actual value.</param>
    public static ValueStatus<TUnderlying> Comptime<TUnderlying>(TUnderlying value) => new ValueStatus<TUnderlying>.ComptimeValue(value);

    /// <summary>
    /// Status for a value that is known at run-time.
    /// </summary>
    public static ValueStatus<TUnderlying> Runtime<TUnderlying>() => ValueStatus<TUnderlying>.RuntimeValue.Instance;

    /// <summary>
    /// Status for a garbage value, i.e. neither known at compile-time or runtime.
    /// </summary>
    public static ValueStatus<TUnderlying> Garbage<TUnderlying>() => ValueStatus<TUnderlying>.GarbageValue.Instance;

    /// <summary>
    /// Status for a semantically invalid value that causes a compilation error.
    /// </summary>
    public static ValueStatus<TUnderlying> Invalid<TUnderlying>() => ValueStatus<TUnderlying>.RuntimeValue.Instance;
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
}

interface RealValue : Value<RealType, decimal>;

sealed class RealValueImpl(RealType type, ValueStatus<decimal> value) : ValueImpl<RealValueImpl, RealType, decimal>(type, value), RealValue
{
    protected override RealValueImpl Clone(ValueStatus<decimal> value) => new(Type, value);
}

interface StringValue : Value<StringType, string>;

sealed class StringValueImpl(StringType type, ValueStatus<string> value) : ValueImpl<StringValueImpl, StringType, string>(type, value), StringValue
{
    protected override StringValueImpl Clone(ValueStatus<string> value) => new(Type, value);
}

sealed class StructureValue(StructureType type, ValueStatus<IReadOnlyDictionary<Identifier, Value>> value) : ValueImpl<StructureValue, StructureType, IReadOnlyDictionary<Identifier, Value>>(type, value)
{
    protected override StructureValue Clone(ValueStatus<IReadOnlyDictionary<Identifier, Value>> value) => new(Type, value);
}

sealed class UnknownValue(UnknownType type, ValueStatus value) : ValueImpl<UnknownType>(type, value);

sealed class ArrayValue(ArrayType type, ValueStatus<Value[]> value) : ValueImpl<ArrayValue, ArrayType, Value[]>(type, value)
{
    protected override ArrayValue Clone(ValueStatus<Value[]> value) => new(Type, value);
}

sealed class BooleanValue(BooleanType type, ValueStatus<bool> value) : ValueImpl<BooleanValue, BooleanType, bool>(type, value)
{
    protected override BooleanValue Clone(ValueStatus<bool> value) => new(Type, value);
}

sealed class CharacterValue(CharacterType type, ValueStatus<char> value) : ValueImpl<CharacterValue, CharacterType, char>(type, value)
{
    protected override CharacterValue Clone(ValueStatus<char> value) => new(Type, value);
}

sealed class FileValue(FileType type, ValueStatus value) : ValueImpl<FileType>(type, value);

sealed class LengthedStringValue(LengthedStringType type, ValueStatus<string> value)
        : ValueImpl<LengthedStringValue, LengthedStringType, string>(type, value), StringValue
{
    StringType Value<StringType, string>.Type => StringType.Instance;

    protected override LengthedStringValue Clone(ValueStatus<string> value) => new(Type, value);
}
