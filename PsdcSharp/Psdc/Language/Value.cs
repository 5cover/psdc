using Scover.Psdc.Parsing;

namespace Scover.Psdc.Language;

interface Value : EquatableSemantics<Value>
{
    EvaluatedType Type { get; }
    ValueStatus Value { get; }

    /// <summary>
    /// A value that is known at compile-time.
    /// </summary>
    /// <typeparam name="TUnderlying">The type of the underlying value.</typeparam>
    /// <param name="value">The actual value.</param>
    public static ValueStatus<TUnderlying> Comptime<TUnderlying>(TUnderlying value) => new ValueStatus<TUnderlying>.ComptimeValue(value);

    /// <summary>
    /// A value that is known at run-time.
    /// </summary>
    public static ValueStatus<TUnderlying> Runtime<TUnderlying>() => new ValueStatus<TUnderlying>.RuntimeValue();

    /// <summary>
    /// A value that is uninitialized, i.e. neither known at compile-time or runtime.
    /// </summary>
    public static ValueStatus<TUnderlying> Uninitialized<TUnderlying>() => new ValueStatus<TUnderlying>.UninitializedValue();
}

interface Value<TUnderlying> : Value
{
    new ValueStatus<TUnderlying> Value { get; }

}

interface Value<out TType, TUnderlying> : Value<TUnderlying> where TType : EvaluatedType
{
    new TType Type { get; }
}

sealed class IntegerValue(IntegerType type, ValueStatus<int> value)
: ValueImpl<IntegerType, int>(type, value), RealValue
{
    RealType Value<RealType, decimal>.Type => RealType.Instance;
    ValueStatus<decimal> Value<decimal>.Value => Value.Map(v => (decimal)v);
}

interface RealValue : Value<RealType, decimal>;

sealed class RealValueImpl(RealType type, ValueStatus<decimal> value) : ValueImpl<RealType, decimal>(type, value), RealValue;

interface StringValue : Value<StringType, string>;

sealed class StringValueImpl(StringType type, ValueStatus<string> value) : ValueImpl<StringType, string>(type, value), StringValue;

sealed class StructureValue(StructureType type, ValueStatus<IReadOnlyDictionary<Identifier, Value>> value) : ValueImpl<StructureType, IReadOnlyDictionary<Identifier, Value>>(type, value);

sealed class UnknownValue(UnknownType type, ValueStatus value) : ValueImpl<UnknownType>(type, value);

sealed class ArrayValue(ArrayType type, ValueStatus<Value[]> value) : ValueImpl<ArrayType, Value[]>(type, value);

sealed class BooleanValue(BooleanType type, ValueStatus<bool> value) : ValueImpl<BooleanType, bool>(type, value);

sealed class CharacterValue(CharacterType type, ValueStatus<char> value) : ValueImpl<CharacterType, char>(type, value);

sealed class FileValue(FileType type, ValueStatus value) : ValueImpl<FileType>(type, value);

sealed class LengthedStringValue(LengthedStringType type, ValueStatus<string> value)
        : ValueImpl<LengthedStringType, string>(type, value), StringValue
{
    StringType Value<StringType, string>.Type => StringType.Instance;
}
