using System.Diagnostics;
using System.Text;

using Scover.Psdc.Parsing;
using static Scover.Psdc.StaticAnalysis.SemanticNode;

namespace Scover.Psdc.Language;

interface InstantiableType<TValue, TUnderlying> : EvaluatedType<TValue>
where TValue : Value
{
    /// <summary>
    /// Instantiate this type.
    /// </summary>
    /// <param name="value">The underlying value of the new instance.</param>
    /// <returns>An instance of this type.</returns>
    public TValue Instantiate(TUnderlying value);
}

interface EvaluatedType : EquatableSemantics<EvaluatedType>
{
    /// <summary>
    /// Get the alias used to refer to this type indirectly.
    /// </summary>
    /// <value>The alias used to refer to this type indirectly or <see langword="null"/> if this type is not aliased.</value>
    public Identifier? Alias { get; }

    /// <summary>
    /// Get the Pseudocode representation of this type.
    /// </summary>
    /// <value>The Pseudocode code that would result in an equivalent <see cref="EvaluatedType"/> object if parsed.</value>
    public string Representation { get; }

    // Static methods used for method groups
    public static bool IsConvertibleTo(EvaluatedType self, EvaluatedType other) => self.IsConvertibleTo(other);
    public static bool IsAssignableTo(EvaluatedType self, EvaluatedType other) => self.IsAssignableTo(other);

    /// <summary>
    /// Is this type implicitly convertible to another type?
    /// </summary>
    /// <param name="other">Another type to compare with the current type.</param>
    /// <returns>This type is implicitly convertible to <paramref name="other"/></returns>
    /// <remarks>Overrides will usually want to call base method in <see cref="EvaluatedType"/> in an logical disjunction.</remarks>
    public bool IsConvertibleTo(EvaluatedType other);

    /// <summary>
    /// Can a value of this type be assigned to a variable of another type?
    /// </summary>
    /// <param name="other">The type of the variable a value of this type would be assigned to.</param>
    /// <returns>This type is assignable to <paramref name="other"/></returns>
    /// <remarks>Overrides will usually want to call base in <see cref="EvaluatedTypeImpl"/> in an logical disjunction.</remarks>
    public bool IsAssignableTo(EvaluatedType other);
    public string ToString();

    /// <summary>
    /// Get the value for this type that is not known at compile-time.
    /// </summary>
    /// <value>A value of this type, whose status is always <see cref="ValueStatus.Runtime"/>.</value>
    public Value RuntimeValue { get; }

    /// <summary>
    /// Get the value for this type that is not known, either at run-time or compile-time.
    /// </summary>
    /// <value>A value of this type, whose  is always <see cref="ValueStatus.Garbage"/>.</value>
    public Value GarbageValue { get; }

    /// <summary>
    /// Get the default value for this type.
    /// </summary>
    /// <value>A value of the this type, whose status is always <see cref="ValueStatus.Garbage"/> or <see cref="ValueStatus.Runtime"/>.</value>
    public Value DefaultValue { get; }

    /// <summary>
    /// Clone this type, but with an alias
    /// </summary>
    /// <param name="alias">A type alias name.</param>
    /// <returns>A clone of this type, but with the <see cref="Alias"/> property set to <paramref name="alias"/>.</returns>
    public EvaluatedType ToAliasReference(Identifier alias);
}

interface EvaluatedType<out TValue> : EvaluatedType where TValue : Value
{
    /// <inheritdoc cref="EvaluatedType.RuntimeValue"/>
    public new TValue RuntimeValue { get; }
    /// <inheritdoc cref="EvaluatedType.GarbageValue"/>
    public new TValue GarbageValue { get; }
    /// <inheritdoc cref="EvaluatedType.DefaultValue"/>
    public new TValue DefaultValue { get; }
}

/// <summary>
/// A type evaluated during static analysis.
/// </summary>
abstract class EvaluatedTypeImpl<TValue>(Identifier? alias) : EvaluatedType<TValue> where TValue : Value
{
    public Identifier? Alias { get; } = alias;

    public string Representation => Alias?.Name ?? ActualRepresentation;

    protected abstract string ActualRepresentation { get; }

    public virtual bool IsConvertibleTo(EvaluatedType other) => SemanticsEqual(other);

    public virtual bool IsAssignableTo(EvaluatedType other) => IsConvertibleTo(other) || other is EvaluatedTypeImpl<TValue> o && o.IsConvertibleFrom(this);

    /// <summary>
    /// Is another type implicitly convertible to this type?
    /// </summary>
    /// <param name="other">Another type to compare with the current type.</param>
    /// <returns><paramref name="other"/> is implicitly convertible to this type.</returns>
    protected virtual bool IsConvertibleFrom(EvaluatedTypeImpl<TValue> other) => false;

    public abstract TValue RuntimeValue { get; }
    public abstract TValue GarbageValue { get; }

    public abstract bool SemanticsEqual(EvaluatedType other);

    public abstract EvaluatedType ToAliasReference(Identifier alias);

    public override string ToString() => Representation;

    /// <summary>
    /// Get the default value for this type.
    /// </summary>
    public abstract TValue DefaultValue { get; }
    Value EvaluatedType.RuntimeValue => RuntimeValue;
    Value EvaluatedType.GarbageValue => GarbageValue;
    Value EvaluatedType.DefaultValue => DefaultValue;
}

internal sealed class ArrayType : EvaluatedTypeImpl<ArrayValue>, InstantiableType<ArrayValue, Value[]>
{
    ArrayType(EvaluatedType elementType, IReadOnlyList<ConstantExpression<int>> dimensions, Identifier? alias) : base(alias)
    {
        ItemType = elementType;
        Dimensions = dimensions;

        DefaultValue = Instantiate(CreateArray(elementType.DefaultValue, dimensions.Select(d => d.Value)));
        RuntimeValue = new(this, Value.Runtime<Value[]>());
        GarbageValue = new(this, Value.Garbage<Value[]>());
    }

    private static Value[] CreateArray(Value value, IEnumerable<int> dimensions)
    {
        var array = new Value[dimensions.Product()];
        System.Array.Fill(array, value);
        return array;
    }

    public override ArrayValue RuntimeValue { get; }
    public override ArrayValue GarbageValue { get; }
    public override ArrayValue DefaultValue { get; }

    public IReadOnlyList<ConstantExpression<int>> Dimensions { get; }

    public EvaluatedType ItemType { get; }

    protected override string ActualRepresentation
     => $"tableau [{string.Join(", ", Dimensions)}] de {ItemType.Representation}";


    public ArrayType(EvaluatedType itemType,
        IReadOnlyList<ConstantExpression<int>> dimensions)
      : this(itemType, dimensions, null) { }

    // Arrays can't be reassigned.
    public override bool IsAssignableTo(EvaluatedType other) => false;

    public override ArrayType ToAliasReference(Identifier alias)
     => new(ItemType, Dimensions, alias);

    public override bool SemanticsEqual(EvaluatedType other) => other is ArrayType o
     && o.ItemType.SemanticsEqual(ItemType)
     && o.Dimensions.AllZipped(Dimensions, (od, d) => od == d);

    /// <param name="value">A flat array of values whose length is the product of all dimensions.</param>
    /// <inheritdoc/>
    public ArrayValue Instantiate(Value[] value)
    {
        Debug.Assert(value.Length == Dimensions.Select(d => d.Value).Product() && value.All(v => v.Type.IsConvertibleTo(ItemType)));
        return new(this, Value.Comptime(value));
    }
}

internal sealed class BooleanType : EvaluatedTypeImpl<BooleanValue>, InstantiableType<BooleanValue, bool>
{
    BooleanType(Identifier? alias) : base(alias)
    {
        DefaultValue = Instantiate(false);
        RuntimeValue = new(this, Value.Runtime<bool>());
        GarbageValue = new(this, Value.Garbage<bool>());
    }

    public static BooleanType Instance { get; } = new(null);

    protected override string ActualRepresentation => "booléen";
    public override BooleanValue RuntimeValue { get; }
    public override BooleanValue GarbageValue { get; }
    public override BooleanValue DefaultValue { get; }

    public override BooleanType ToAliasReference(Identifier alias) => new(alias);

    public override bool SemanticsEqual(EvaluatedType other) => other is BooleanType;

    public BooleanValue Instantiate(bool value) => new(this, Value.Comptime(value));
}

internal sealed class CharacterType : EvaluatedTypeImpl<CharacterValue>, InstantiableType<CharacterValue, char>
{
    CharacterType(Identifier? alias) : base(alias)
    {
        DefaultValue = Instantiate('\0');
        RuntimeValue = new(this, Value.Runtime<char>());
        GarbageValue = new(this, Value.Garbage<char>());
    }

    public override CharacterValue RuntimeValue { get; }
    public override CharacterValue GarbageValue { get; }
    public static CharacterType Instance { get; } = new(null);

    protected override string ActualRepresentation => "caractère";

    public override CharacterValue DefaultValue { get; }

    public override CharacterType ToAliasReference(Identifier alias) => new(alias);

    public override bool SemanticsEqual(EvaluatedType other) => other is CharacterType;

    public CharacterValue Instantiate(char value) => new(this, Value.Comptime(value));
}

internal sealed class FileType : EvaluatedTypeImpl<FileValue>
{
    FileType(Identifier? alias) : base(alias)
    {
        DefaultValue = GarbageValue = new(this, ValueStatus.Garbage);
        RuntimeValue = new(this, ValueStatus.Runtime);
    }

    public override FileValue RuntimeValue { get; }
    public override FileValue GarbageValue { get; }
    public static FileType Instance { get; } = new(null);
    protected override string ActualRepresentation => "nomFichierLog";

    public override FileValue DefaultValue { get; }

    public override FileType ToAliasReference(Identifier alias) => new(alias);

    public override bool SemanticsEqual(EvaluatedType other) => other is FileType;
}

internal sealed class LengthedStringType : EvaluatedTypeImpl<LengthedStringValue>, InstantiableType<LengthedStringValue, string>
{
    LengthedStringType(Option<Expression> lengthExpression, int length, Identifier? alias = null) : base(alias)
    {
        LengthConstantExpression = lengthExpression;
        Length = length;
        DefaultValue = GarbageValue = new(this, Value.Garbage<string>());
        RuntimeValue = new(this, Value.Runtime<string>());
    }

    public int Length { get; }

    public override LengthedStringValue RuntimeValue { get; }
    public override LengthedStringValue GarbageValue { get; }
    public Option<Expression> LengthConstantExpression { get; }

    protected override string ActualRepresentation => $"chaîne({Length})";

    public override LengthedStringValue DefaultValue { get; }

    public static LengthedStringType Create(ConstantExpression<int> length)
     => new(length.Expression.Some(), length.Value);

    public static LengthedStringType Create(int length)
     => new(Option.None<Expression>(), length, null);

    public override bool IsConvertibleTo(EvaluatedType other) => base.IsConvertibleTo(other) || other is StringType;
    public override bool IsAssignableTo(EvaluatedType other) => base.IsAssignableTo(other) || other is LengthedStringType o && o.Length >= Length;
    public override bool SemanticsEqual(EvaluatedType other) => other is LengthedStringType o && o.Length == Length;

    public override LengthedStringType ToAliasReference(Identifier alias) => new(LengthConstantExpression, Length, alias);

    public LengthedStringValue Instantiate(string value)
    {
        Debug.Assert(value.Length == Length);
        return new(this, Value.Comptime(value));
    }
}

internal sealed class IntegerType : EvaluatedTypeImpl<IntegerValue>, InstantiableType<IntegerValue, int>
{
    public override IntegerValue RuntimeValue { get; }
    public override IntegerValue GarbageValue { get; }
    IntegerType(Identifier? alias) : base(alias)
    {
        DefaultValue = Instantiate(0);
        RuntimeValue = new(this, Value.Runtime<int>());
        GarbageValue = new(this, Value.Garbage<int>());
    }

    public static IntegerType Instance { get; } = new(null);
    protected override string ActualRepresentation => "entier";

    public override IntegerValue DefaultValue { get; }

    public override IntegerType ToAliasReference(Identifier alias) => new(alias);

    public override bool IsConvertibleTo(EvaluatedType other) => base.IsConvertibleTo(other) || other is RealType;

    public override bool SemanticsEqual(EvaluatedType other) => other is IntegerType;

    public IntegerValue Instantiate(int value) => new(this, Value.Comptime(value));
}

internal sealed class RealType : EvaluatedTypeImpl<RealValue>, InstantiableType<RealValue, decimal>
{
    RealType(Identifier? alias) : base(alias)
    {
        DefaultValue = Instantiate(0m);
        RuntimeValue = new RealValueImpl(this, Value.Runtime<decimal>());
        GarbageValue = new RealValueImpl(this, Value.Garbage<decimal>());
    }
    public override RealValue RuntimeValue { get; }
    public override RealValue GarbageValue { get; }
    public static RealType Instance { get; } = new(null);
    protected override string ActualRepresentation => "réel";

    public override RealValue DefaultValue { get; }

    public override RealType ToAliasReference(Identifier alias) => new(alias);

    public override bool SemanticsEqual(EvaluatedType other) => other is RealType;

    public RealValue Instantiate(decimal value) => new RealValueImpl(this, Value.Comptime(value));
}

internal class StringType : EvaluatedTypeImpl<StringValue>, InstantiableType<StringValue, string>
{
    StringType(Identifier? alias) : base(alias)
    {
        DefaultValue = GarbageValue = new StringValueImpl(this, Value.Garbage<string>());
        RuntimeValue = new StringValueImpl(this, Value.Runtime<string>());
    }

    public override StringValue RuntimeValue { get; }
    public override StringValue GarbageValue { get; }
    public static StringType Instance { get; } = new(null);

    protected override string ActualRepresentation => "chaîne";

    public override StringValue DefaultValue { get; }

    public override StringType ToAliasReference(Identifier alias) => new(alias);

    public override bool SemanticsEqual(EvaluatedType other) => other is StringType;

    public StringValue Instantiate(string value) => new StringValueImpl(this, Value.Comptime(value));
}

internal sealed class StructureType : EvaluatedTypeImpl<StructureValue>, InstantiableType<StructureValue, IReadOnlyDictionary<Identifier, Value>>
{
    public StructureType(OrderedMap<Identifier, EvaluatedType> components, Identifier? alias = null) : base(alias)
    {
        Components = components;
        _representation = alias is null
                    ? new(() => {
                        StringBuilder sb = new();
                        sb.AppendLine("structure début");
                        foreach (var comp in components.Map) {
                            sb.AppendLine($"{comp.Key} : {comp.Value.Representation};");
                        }
                        sb.Append("fin");
                        return sb.ToString();
                    })
                    : new(alias.Name); // To avoid long type representations, use the alias name if available.
        DefaultValue = Instantiate(components.Map.ToDictionary(kv => kv.Key, kv => kv.Value.DefaultValue));
        RuntimeValue = new(this, Value.Runtime<IReadOnlyDictionary<Identifier, Value>>());
        GarbageValue = new(this, Value.Garbage<IReadOnlyDictionary<Identifier, Value>>());
    }

    readonly Lazy<string> _representation;

    public OrderedMap<Identifier, EvaluatedType> Components { get; }
    protected override string ActualRepresentation => _representation.Value;
    public override StructureValue RuntimeValue { get; }
    public override StructureValue GarbageValue { get; }
    public override StructureValue DefaultValue { get; }

    public override StructureType ToAliasReference(Identifier alias) => new(Components, alias);

    public override bool SemanticsEqual(EvaluatedType other) => other is StructureType o
     && o.Components.Map.Keys.AllSemanticsEqual(Components.Map.Keys)
     && o.Components.Map.Values.AllSemanticsEqual(Components.Map.Values);

    public StructureValue Instantiate(IReadOnlyDictionary<Identifier, Value> value)
    {
        Dictionary<Identifier, Value> completedValue = new(value);
        completedValue.CheckKeys(Components.Map.Keys.ToList(),
            missingKey => Components.Map[missingKey].GarbageValue,
            excessKey => Debug.Fail($"Excess key: `{excessKey}`"));
        return new(this, Value.Comptime(value));
    }
}

internal sealed class UnknownType : EvaluatedTypeImpl<UnknownValue>
{
    UnknownType(SourceTokens sourceTokens, string repr, Identifier? alias = null) : base(alias)
    {
        SourceTokens = sourceTokens;
        ActualRepresentation = repr;
        DefaultValue = GarbageValue = new(this, ValueStatus.Garbage);
        RuntimeValue = new(this, ValueStatus.Runtime);
    }

    public override UnknownValue RuntimeValue { get; }
    public override UnknownValue GarbageValue { get; }
    protected override string ActualRepresentation { get; }
    public SourceTokens SourceTokens { get; }

    public static UnknownType Inferred { get; } = new UnknownType(SourceTokens.Empty, "<unknown-type>");
    public override UnknownValue DefaultValue { get; }

    public static UnknownType Declared(string input, SourceTokens sourceTokens) => new(sourceTokens, input[sourceTokens.InputRange]);

    // Provied an alternative overload which returns a more generic type for implicit operators
    public static UnknownType Declared(string input, Node node) => Declared(input, node.SourceTokens);

    public override UnknownType ToAliasReference(Identifier alias) => new(SourceTokens, ActualRepresentation, alias);

    // Unknown is convertible to every other type, and every type is convertible to Unknwon.
    // This is to prevent cascading errors when an object of an unknown type is used.
    public override bool IsConvertibleTo(EvaluatedType other) => true;
    protected override bool IsConvertibleFrom(EvaluatedTypeImpl<UnknownValue> other) => true;

    public override bool SemanticsEqual(EvaluatedType other) => other is UnknownType o
     && o.SourceTokens.SequenceEqual(SourceTokens);
}

