using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

using Scover.Psdc.Parsing;
using static Scover.Psdc.StaticAnalysis.SemanticNode;

namespace Scover.Psdc.Pseudocode;

interface InstantiableType<TValue, TUnderlying> : EvaluatedType<TValue>
where TValue : Value
{
    /// <summary>
    /// Instanciate this type.
    /// </summary>
    /// <param name="value">The underlying value of the new instance.</param>
    /// <returns>An instance of this type.</returns>
    public TValue Instanciate(TUnderlying value);
}

interface EvaluatedType : IFormattableUsable, EquatableSemantics<EvaluatedType>
{
    /// <summary>
    /// Get the alias used to refer to this type indirectly.
    /// </summary>
    /// <value>The alias used to refer to this type indirectly or <see langword="null"/> if this type is not aliased.</value>
    public Option<Identifier> Alias { get; }

    // Static methods used for method groups
    public static bool IsConvertibleTo(EvaluatedType self, EvaluatedType other) => self.IsConvertibleTo(other);
    public static bool IsAssignableTo(EvaluatedType self, EvaluatedType other) => self.IsAssignableTo(other);

    /// <summary>
    /// Is this type implicitly convertible to another type?
    /// </summary>
    /// <param name="other">Another type to compare with the current type.</param>
    /// <returns>This type is implicitly convertible to <paramref name="other"/>.</returns>
    /// <remarks>Overrides will usually want to call base method in <see cref="EvaluatedType"/> in an logical disjunction.</remarks>
    public bool IsConvertibleTo(EvaluatedType other);

    /// <summary>
    /// Can a value of this type be assigned to a variable of another type?
    /// </summary>
    /// <param name="other">The type of the variable a value of this type would be assigned to.</param>
    /// <returns>This type is assignable to <paramref name="other"/>.</returns>
    /// <remarks>Overrides will usually want to call base in <see cref="EvaluatedTypeImpl"/> in an logical disjunction.</remarks>
    public bool IsAssignableTo(EvaluatedType other);

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
    /// <value>A value of this type, whose status is always <see cref="ValueStatus.Garbage"/> or <see cref="ValueStatus.Comptime"/>. Represents the default value to set to in an initializer.</value>
    public Value DefaultValue { get; }

    /// <summary>
    /// Gets the invalid value for this type
    /// </summary>
    /// <value>A value of this types, whose status is always <see cref="ValueStatus.Invalid"/>.</value>
    public Value InvalidValue { get; }

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
    /// <inheritdoc cref="EvaluatedType.InvalidValue"/>
    public new TValue InvalidValue { get; }
}

sealed class ArrayType : EvaluatedTypeImplInstantiable<ArrayValue, ImmutableArray<Value>>
{
    ArrayType(EvaluatedType itemType, ComptimeExpression<int> length, ValueOption<Identifier> alias)
     : base(alias, CreateDefaultValue(itemType, length))
    {
        ItemType = itemType;
        Length = length;
    }

    public ComptimeExpression<int> Length { get; }

    public EvaluatedType ItemType { get; }

    public ImmutableArray<Value> CreateDefaultValue() => CreateDefaultValue(ItemType, Length);
    static ImmutableArray<Value> CreateDefaultValue(EvaluatedType type, ComptimeExpression<int> length) => Enumerable.Repeat(type.DefaultValue, length.Value).ToImmutableArray();

    public ArrayType(EvaluatedType itemType,
        ComptimeExpression<int> length)
      : this(itemType, length, default) { }

    public override ArrayType ToAliasReference(Identifier alias)
     => new(ItemType, Length, alias);

    public override bool SemanticsEqual(EvaluatedType other) => other is ArrayType o
     && o.ItemType.SemanticsEqual(ItemType)
     && o.Length.Value == Length.Value;

    protected override ArrayValue CreateValue(ValueStatus<ImmutableArray<Value>> status)
    {
        Debug.Assert(status.ComptimeValue is not { HasValue: true } c
                  || c.Value.Length == Length.Value
                  && c.Value.All(v => v.Type.IsConvertibleTo(ItemType)));
        return new(this, status);
    }

    private ImmutableList<ComptimeExpression<int>>? _dimensions;
    private IReadOnlyList<ComptimeExpression<int>> Dimensions => _dimensions ??= GetDimensions();

    private ImmutableList<ComptimeExpression<int>> GetDimensions()
    {
        var l = ImmutableList.Create(Length);
        return ItemType is ArrayType arr
            ? l.AddRange(arr.Dimensions)
            : l;
    }

    private EvaluatedType? _innermostItemType;
    private EvaluatedType InnermostItemType => _innermostItemType ??= GetInnermostItemType();
    private EvaluatedType GetInnermostItemType()
     => ItemType is ArrayType arr
        ? arr.InnermostItemType
        : ItemType;

    protected override string ToStringNoAlias(IFormatProvider? fmtProvider)
     => string.Create(fmtProvider, $"tableau [{string.Join(", ", Dimensions)}] de {InnermostItemType}");
}

sealed class BooleanType : EvaluatedTypeImplInstantiable<BooleanValue, bool>
{
    BooleanType(ValueOption<Identifier> alias) : base(alias, false)
    { }

    public static BooleanType Instance { get; } = new(default);
    public override BooleanType ToAliasReference(Identifier alias) => new(alias);

    public override bool SemanticsEqual(EvaluatedType other) => other is BooleanType;
    protected override BooleanValue CreateValue(ValueStatus<bool> status) => new(this, status);
    protected override string ToStringNoAlias(IFormatProvider? fmtProvider) => "booléen";
}

sealed class CharacterType : EvaluatedTypeImplInstantiable<CharacterValue, char>
{
    CharacterType(ValueOption<Identifier> alias) : base(alias, '\0')
    { }

    public static CharacterType Instance { get; } = new(default);
    public override CharacterType ToAliasReference(Identifier alias) => new(alias);
    public override bool SemanticsEqual(EvaluatedType other) => other is CharacterType;
    protected override CharacterValue CreateValue(ValueStatus<char> status) => new(this, status);
    protected override string ToStringNoAlias(IFormatProvider? fmtProvider) => "caractère";
}

sealed class FileType : EvaluatedTypeImplNotInstantiable<FileValue>
{
    FileType(ValueOption<Identifier> alias) : base(alias, ValueStatus.Garbage.Instance)
    { }

    public static FileType Instance { get; } = new(default);
    public override FileType ToAliasReference(Identifier alias) => new(alias);

    public override bool SemanticsEqual(EvaluatedType other) => other is FileType;
    protected override FileValue CreateValue(ValueStatus status) => new(this, status);
    protected override string ToStringNoAlias(IFormatProvider? fmtProvider) => "nomFichierLog";
}

sealed class LengthedStringType : EvaluatedTypeImplInstantiable<LengthedStringValue, string>
{
    LengthedStringType(Option<Expression> lengthExpression, int length, ValueOption<Identifier> alias = default) : base(alias, ValueStatus.Garbage<string>.Instance)
    {
        LengthConstantExpression = lengthExpression;
        Length = length;
    }

    public int Length { get; }
    public Option<Expression> LengthConstantExpression { get; }
    public static LengthedStringType Create(ComptimeExpression<int> length)
     => new(length.Expression.Some(), length.Value);
    public static LengthedStringType Create(int length)
     => new(Option.None<Expression>(), length, default);

    public override bool IsConvertibleTo(EvaluatedType other) => base.IsConvertibleTo(other) || other is StringType;
    public override bool IsAssignableTo(EvaluatedType other) => base.IsAssignableTo(other) || other is LengthedStringType o && o.Length >= Length;
    public override bool SemanticsEqual(EvaluatedType other) => other is LengthedStringType o && o.Length == Length;

    public override LengthedStringType ToAliasReference(Identifier alias) => new(LengthConstantExpression, Length, alias);
    protected override LengthedStringValue CreateValue(ValueStatus<string> status)
    {
        Debug.Assert(status.ComptimeValue is not { HasValue: true } c || c.Value.Length == Length);
        return new(this, status);
    }

    protected override string ToStringNoAlias(IFormatProvider? fmtProvider) => string.Create(fmtProvider, $"chaîne({Length})");
}

sealed class IntegerType : EvaluatedTypeImplInstantiable<IntegerValue, int>
{
    IntegerType(ValueOption<Identifier> alias) : base(alias, 0)
    { }

    public static IntegerType Instance { get; } = new(default);
    public override IntegerType ToAliasReference(Identifier alias) => new(alias);

    public override bool IsConvertibleTo(EvaluatedType other) => base.IsConvertibleTo(other) || other is RealType;

    public override bool SemanticsEqual(EvaluatedType other) => other is IntegerType;
    protected override IntegerValue CreateValue(ValueStatus<int> status) => new(this, status);
    protected override string ToStringNoAlias(IFormatProvider? fmtProvider) => "entier";
}

sealed class RealType : EvaluatedTypeImplInstantiable<RealValue, decimal>
{
    RealType(ValueOption<Identifier> alias) : base(alias, 0m)
    { }
    public static RealType Instance { get; } = new(default);
    public override RealType ToAliasReference(Identifier alias) => new(alias);
    public override bool SemanticsEqual(EvaluatedType other) => other is RealType;
    protected override RealValue CreateValue(ValueStatus<decimal> status) => new RealValueImpl(this, status);
    protected override string ToStringNoAlias(IFormatProvider? fmtProvider) => "réel";
}

sealed class StringType : EvaluatedTypeImplInstantiable<StringValue, string>
{
    StringType(ValueOption<Identifier> alias) : base(alias, ValueStatus.Garbage<string>.Instance)
    { }

    public static StringType Instance { get; } = new(default);

    public override StringType ToAliasReference(Identifier alias) => new(alias);

    public override bool SemanticsEqual(EvaluatedType other) => other is StringType;
    protected override StringValue CreateValue(ValueStatus<string> status) => new StringValueImpl(this, status);
    protected override string ToStringNoAlias(IFormatProvider? fmtProvider) => "chaîne";
}

sealed class StructureType : EvaluatedTypeImplInstantiable<StructureValue, ImmutableDictionary<Identifier, Value>>
{
    const int MaxComponentsInRepresentation = 3;
    public StructureType(OrderedMap<Identifier, EvaluatedType> components, ValueOption<Identifier> alias = default)
     : base(alias, CreateDefaultValue(components.Map))
     => Components = components;

    public OrderedMap<Identifier, EvaluatedType> Components { get; }
    public override StructureType ToAliasReference(Identifier alias) => new(Components, alias);

    public override bool SemanticsEqual(EvaluatedType other) => other is StructureType o
     && o.Components.Map.Keys.AllSemanticsEqual(Components.Map.Keys)
     && o.Components.Map.Values.AllSemanticsEqual(Components.Map.Values);

    public ImmutableDictionary<Identifier, Value> CreateDefaultValue() => CreateDefaultValue(Components.Map);

    public static ImmutableDictionary<Identifier, Value> CreateDefaultValue(IReadOnlyDictionary<Identifier, EvaluatedType> components)
     => components.ToImmutableDictionary(kv => kv.Key, kv => kv.Value.DefaultValue);

    protected override StructureValue CreateValue(ValueStatus<ImmutableDictionary<Identifier, Value>> status)
    {
        Debug.Assert(status.ComptimeValue is not { HasValue: true } v
                  || Components.Map.Keys.ToHashSet().SetEquals(v.Value.Keys), "Provided value and struture has different components");
        return new(this, status);
    }

    protected override string ToStringNoAlias(IFormatProvider? fmtProvider)
    {
        var o = new StringBuilder()
            .Append("structure { ")
            .AppendJoin(", ", Components.List.Take(MaxComponentsInRepresentation)
                .Select(c => string.Create(fmtProvider, $"{c.Key}: {c.Value}")));

        var nbExcessComps = Components.Count - MaxComponentsInRepresentation;
        if (nbExcessComps > 0) {
            o.Append(fmtProvider, $", ({nbExcessComps} more...)");
        }
        return o.Append(" }").ToString();
    }
}

sealed class VoidType : EvaluatedTypeImplNotInstantiable<VoidValue>
{
    VoidType(ValueOption<Identifier> alias) : base(alias, ValueStatus.Runtime.Instance)
    { }
    public static VoidType Instance { get; } = new(default);
    public override VoidType ToAliasReference(Identifier alias) => new(alias);
    public override bool SemanticsEqual(EvaluatedType other) => other is VoidType;
    protected override VoidValue CreateValue(ValueStatus status) => new(this, status);
    protected override string ToStringNoAlias(IFormatProvider? fmtProvider) => "<void>";
}

sealed class UnknownType : EvaluatedTypeImplNotInstantiable<UnknownValue>
{
    private readonly string _repr;
    UnknownType(SourceTokens sourceTokens, string repr, ValueOption<Identifier> alias = default) : base(alias, ValueStatus.Invalid.Instance)
     => (SourceTokens, _repr) = (sourceTokens, repr);

    public SourceTokens SourceTokens { get; }

    public static UnknownType Inferred { get; } = new UnknownType(SourceTokens.Empty, "<unknown-type>");

    public static UnknownType Declared(string input, SourceTokens sourceTokens) => new(sourceTokens, input[sourceTokens.InputRange]);

    // Provied an alternative overload which returns a more generic type for implicit operators
    public static UnknownType Declared(string input, Node node) => Declared(input, node.SourceTokens);

    public override UnknownType ToAliasReference(Identifier alias) => new(SourceTokens, _repr, alias);

    // Unknown is convertible to every other type, and every type is convertible to Unknwon.
    // This is to prevent cascading errors when an object of an unknown type is used.
    public override bool IsConvertibleTo(EvaluatedType other) => true;
    protected override bool IsConvertibleFrom(EvaluatedTypeImpl<UnknownValue> other) => true;

    public override bool SemanticsEqual(EvaluatedType other) => other is UnknownType o
     && o.SourceTokens.SequenceEqual(SourceTokens);
    protected override UnknownValue CreateValue(ValueStatus status) => new(this, status);
    protected override string ToStringNoAlias(IFormatProvider? fmtProvider) => _repr;
}
