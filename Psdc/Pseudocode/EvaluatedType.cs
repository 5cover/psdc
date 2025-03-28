using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

using Scover.Psdc.Parsing;

using static Scover.Psdc.StaticAnalysis.SemanticNode;

namespace Scover.Psdc.Pseudocode;

interface InstantiableType<out TValue, in TUnderlying> : EvaluatedType<TValue>
where TValue : Value
{
    /// <summary>
    /// Instanciate this type.
    /// </summary>
    /// <param name="value">The underlying value of the new instance.</param>
    /// <returns>An instance of this type.</returns>
    TValue Instanciate(TUnderlying value);
}
interface EvaluatedType : IFormattableUsable, EquatableSemantics<EvaluatedType>
{
    /// <summary>
    /// Get the alias used to refer to this type indirectly.
    /// </summary>
    /// <value>The alias used to refer to this type indirectly or <see langword="null"/> if this type is not aliased.</value>
    Option<Ident> Alias { get; }

    // Static methods used for method groups
    static bool IsConvertibleTo(EvaluatedType self, EvaluatedType other) => self.IsConvertibleTo(other);
    static bool IsAssignableTo(EvaluatedType self, EvaluatedType other) => self.IsAssignableTo(other);

    /// <summary>
    /// Is this type implicitly convertible to another type?
    /// </summary>
    /// <param name="other">Another type to compare with the current type.</param>
    /// <returns>This type is implicitly convertible to <paramref name="other"/>.</returns>
    /// <remarks>Overrides will usually want to call base method in <see cref="EvaluatedType"/> in a logical disjunction.</remarks>
    bool IsConvertibleTo(EvaluatedType other);

    /// <summary>
    /// Can a value of this type be assigned to a variable of another type?
    /// </summary>
    /// <param name="other">The type of the variable a value of this type would be assigned to.</param>
    /// <returns>This type is assignable to <paramref name="other"/>.</returns>
    /// <remarks>Overrides will usually want to call base in <see cref="EvaluatedTypeImpl{TValue}"/> in a logical disjunction.</remarks>
    bool IsAssignableTo(EvaluatedType other);

    /// <summary>
    /// Get the value for this type that is not known at compile-time.
    /// </summary>
    /// <value>A value of this type, whose status is always <see cref="ValueStatus.Runtime"/>.</value>
    Value RuntimeValue { get; }

    /// <summary>
    /// Get the value for this type that is not known, either at run-time or compile-time.
    /// </summary>
    /// <value>A value of this type, whose  is always <see cref="ValueStatus.Garbage"/>.</value>
    Value GarbageValue { get; }

    /// <summary>
    /// Get the default value for this type.
    /// </summary>
    /// <value>A value of this type, whose status is always <see cref="ValueStatus.Garbage"/> or <see cref="ValueStatus.Comptime"/>. Represents the default value to set to in an initializer.</value>
    Value DefaultValue { get; }

    /// <summary>
    /// Gets the invalid value for this type
    /// </summary>
    /// <value>A value of this type, whose status is always <see cref="ValueStatus.Invalid"/>.</value>
    Value InvalidValue { get; }

    /// <summary>
    /// Clone this type, but with an alias
    /// </summary>
    /// <param name="alias">A type alias name.</param>
    /// <returns>A clone of this type, but with the <see cref="Alias"/> property set to <paramref name="alias"/>.</returns>
    EvaluatedType ToAliasReference(Ident alias);
}
interface EvaluatedType<out TValue> : EvaluatedType where TValue : Value
{
    /// <inheritdoc cref="EvaluatedType.RuntimeValue"/>
    new TValue RuntimeValue { get; }
    /// <inheritdoc cref="EvaluatedType.GarbageValue"/>
    new TValue GarbageValue { get; }
    /// <inheritdoc cref="EvaluatedType.DefaultValue"/>
    new TValue DefaultValue { get; }
    /// <inheritdoc cref="EvaluatedType.InvalidValue"/>
    new TValue InvalidValue { get; }
}
sealed class ArrayType : EvaluatedTypeImplInstantiable<ArrayValue, ImmutableArray<Value>>
{
    ArrayType(EvaluatedType itemType, ComptimeExpression<int> length, ValueOption<Ident> alias)
        : base(alias, CreateDefaultValue(itemType, length))
    {
        ItemType = itemType;
        Length = length;
    }

    public ComptimeExpression<int> Length { get; }

    public EvaluatedType ItemType { get; }

    public ImmutableArray<Value> CreateDefaultValue() => CreateDefaultValue(ItemType, Length);
    static ImmutableArray<Value> CreateDefaultValue(EvaluatedType type, ComptimeExpression<int> length) =>
        [..Enumerable.Repeat(type.DefaultValue, length.Value)];

    public ArrayType(
        EvaluatedType itemType,
        ComptimeExpression<int> length
    )
        : this(itemType, length, default) { }

    public override ArrayType ToAliasReference(Ident alias) => new(ItemType, Length, alias);

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

    ImmutableList<ComptimeExpression<int>>? _dimensions;
    IReadOnlyList<ComptimeExpression<int>> Dimensions => _dimensions ??= GetDimensions();

    ImmutableList<ComptimeExpression<int>> GetDimensions()
    {
        var l = ImmutableList.Create(Length);
        return ItemType is ArrayType arr
            ? l.AddRange(arr.Dimensions)
            : l;
    }

    EvaluatedType? _innermostItemType;
    EvaluatedType InnermostItemType => _innermostItemType ??= GetInnermostItemType();
    EvaluatedType GetInnermostItemType() => ItemType is ArrayType arr
        ? arr.InnermostItemType
        : ItemType;

    protected override string ToStringNoAlias(IFormatProvider? fmtProvider) =>
        string.Create(fmtProvider, $"tableau({InnermostItemType})[{string.Join("][", Dimensions)}]");
}
sealed class BooleanType : EvaluatedTypeImplInstantiable<BooleanValue, bool>
{
    BooleanType(ValueOption<Ident> alias) : base(alias, false) { }

    public static BooleanType Instance { get; } = new(default);
    public override BooleanType ToAliasReference(Ident alias) => new(alias);

    public override bool SemanticsEqual(EvaluatedType other) => other is BooleanType;
    protected override BooleanValue CreateValue(ValueStatus<bool> status) => new(this, status);
    protected override string ToStringNoAlias(IFormatProvider? fmtProvider) => "booléen";
}
sealed class CharacterType : EvaluatedTypeImplInstantiable<CharacterValue, char>
{
    CharacterType(ValueOption<Ident> alias) : base(alias, '\0') { }

    public static CharacterType Instance { get; } = new(default);
    public override CharacterType ToAliasReference(Ident alias) => new(alias);
    public override bool SemanticsEqual(EvaluatedType other) => other is CharacterType;
    protected override CharacterValue CreateValue(ValueStatus<char> status) => new(this, status);
    protected override string ToStringNoAlias(IFormatProvider? fmtProvider) => "caractère";
}
sealed class FileType : EvaluatedTypeImplNotInstantiable<FileValue>
{
    FileType(ValueOption<Ident> alias) : base(alias, ValueStatus.Garbage.Instance) { }

    public static FileType Instance { get; } = new(default);
    public override FileType ToAliasReference(Ident alias) => new(alias);

    public override bool SemanticsEqual(EvaluatedType other) => other is FileType;
    protected override FileValue CreateValue(ValueStatus status) => new(this, status);
    protected override string ToStringNoAlias(IFormatProvider? fmtProvider) => "nomFichierLog";
}
sealed class LengthedStringType : EvaluatedTypeImplInstantiable<LengthedStringValue, string>
{
    LengthedStringType(Option<Expr> lengthExpression, int length, ValueOption<Ident> alias = default) : base(alias, ValueStatus.Garbage<string>.Instance)
    {
        LengthConstantExpression = lengthExpression;
        Length = length;
    }

    public int Length { get; }
    public Option<Expr> LengthConstantExpression { get; }
    public static LengthedStringType Create(ComptimeExpression<int> length) => new(length.Expression.Some(), length.Value);
    public static LengthedStringType Create(int length) => new(Option.None<Expr>(), length);

    public override bool IsConvertibleTo(EvaluatedType other) => base.IsConvertibleTo(other) || other is StringType;
    public override bool IsAssignableTo(EvaluatedType other) => base.IsAssignableTo(other) || other is LengthedStringType o && o.Length >= Length;
    public override bool SemanticsEqual(EvaluatedType other) => other is LengthedStringType o && o.Length == Length;

    public override LengthedStringType ToAliasReference(Ident alias) => new(LengthConstantExpression, Length, alias);
    protected override LengthedStringValue CreateValue(ValueStatus<string> status)
    {
        Debug.Assert(status.ComptimeValue is not { HasValue: true } c || c.Value.Length == Length);
        return new(this, status);
    }

    protected override string ToStringNoAlias(IFormatProvider? fmtProvider) => string.Create(fmtProvider, $"chaîne({Length})");
}
sealed class IntegerType : EvaluatedTypeImplInstantiable<IntegerValue, int>
{
    IntegerType(ValueOption<Ident> alias) : base(alias, 0) { }

    public static IntegerType Instance { get; } = new(default);
    public override IntegerType ToAliasReference(Ident alias) => new(alias);

    public override bool IsConvertibleTo(EvaluatedType other) => base.IsConvertibleTo(other) || other is RealType;

    public override bool SemanticsEqual(EvaluatedType other) => other is IntegerType;
    protected override IntegerValue CreateValue(ValueStatus<int> status) => new(this, status);
    protected override string ToStringNoAlias(IFormatProvider? fmtProvider) => "entier";
}
sealed class RealType : EvaluatedTypeImplInstantiable<RealValue, decimal>
{
    RealType(ValueOption<Ident> alias) : base(alias, 0m) { }
    public static RealType Instance { get; } = new(default);
    public override RealType ToAliasReference(Ident alias) => new(alias);
    public override bool SemanticsEqual(EvaluatedType other) => other is RealType;
    protected override RealValue CreateValue(ValueStatus<decimal> status) => new RealValueImpl(this, status);
    protected override string ToStringNoAlias(IFormatProvider? fmtProvider) => "réel";
}
sealed class StringType : EvaluatedTypeImplInstantiable<StringValue, string>
{
    StringType(ValueOption<Ident> alias) : base(alias, ValueStatus.Garbage<string>.Instance) { }

    public static StringType Instance { get; } = new(default);

    public override StringType ToAliasReference(Ident alias) => new(alias);

    public override bool SemanticsEqual(EvaluatedType other) => other is StringType;
    protected override StringValue CreateValue(ValueStatus<string> status) => new StringValueImpl(this, status);
    protected override string ToStringNoAlias(IFormatProvider? fmtProvider) => "chaîne";
}
sealed class StructureType(ImmutableOrderedMap<Ident, EvaluatedType> components, ValueOption<Ident> alias = default)
    : EvaluatedTypeImplInstantiable<StructureValue, ImmutableOrderedMap<Ident, Value>>(
        alias, CreateDefaultValue(components)
    )
{
    const int MaxComponentsInRepresentation = 3;
    public ImmutableOrderedMap<Ident, EvaluatedType> Components { get; } = components;
    public override StructureType ToAliasReference(Ident alias) => new(Components, alias);

    public override bool SemanticsEqual(EvaluatedType other) => other is StructureType o
                                                             && o.Components.Map.Keys.AllEqual(Components.Map.Keys)
                                                             && o.Components.Map.Values.AllSemanticsEqual(Components.Map.Values);

    public ImmutableOrderedMap<Ident, Value> CreateDefaultValue() => CreateDefaultValue(Components);

    public static ImmutableOrderedMap<Ident, Value> CreateDefaultValue(ImmutableOrderedMap<Ident, EvaluatedType> components) =>
        new(components.List.Select(kvp => KeyValuePair.Create(kvp.Key, kvp.Value.DefaultValue)).ToImmutableList());

    protected override StructureValue CreateValue(ValueStatus<ImmutableOrderedMap<Ident, Value>> status)
    {
        Debug.Assert(status.ComptimeValue is not { HasValue: true } v
                  || Components.Map.Keys.ToHashSet().SetEquals(v.Value.Map.Keys), "Provided value and struture has different components");
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
    VoidType(ValueOption<Ident> alias) : base(alias, ValueStatus.Runtime.Instance) { }
    public static VoidType Instance { get; } = new(default);
    public override VoidType ToAliasReference(Ident alias) => new(alias);
    public override bool SemanticsEqual(EvaluatedType other) => other is VoidType;
    protected override VoidValue CreateValue(ValueStatus status) => new(this, status);
    protected override string ToStringNoAlias(IFormatProvider? fmtProvider) => "<void>";
}
sealed class UnknownType : EvaluatedTypeImplNotInstantiable<UnknownValue>
{
    readonly string _repr;
    UnknownType(Range location, string repr, ValueOption<Ident> alias = default) : base(alias, ValueStatus.Invalid.Instance) =>
        (Location, _repr) = (location, repr);

    public Range Location { get; }
    public static UnknownType Inferred { get; } = new(default, "<unknown-type>");

    public static UnknownType Declared(string input, Range location) => new(location, input[location]);

    public override UnknownType ToAliasReference(Ident alias) => new(Location, _repr, alias);

    // Unknown is convertible to every other type, and every type is convertible to Unknwon.
    // This is to prevent cascading errors when an object of an unknown type is used.
    public override bool IsConvertibleTo(EvaluatedType other) => true;
    protected override bool IsConvertibleFrom(EvaluatedTypeImpl<UnknownValue> other) => true;

    public override bool SemanticsEqual(EvaluatedType other) => other is UnknownType o
                                                             && o._repr == _repr;

    protected override UnknownValue CreateValue(ValueStatus status) => new(this, status);
    protected override string ToStringNoAlias(IFormatProvider? fmtProvider) => _repr;
}
