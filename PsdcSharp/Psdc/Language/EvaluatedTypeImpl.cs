using Scover.Psdc.Parsing;

namespace Scover.Psdc.Language;

abstract class EvaluatedTypeImpl<TValue> : EvaluatedType<TValue> where TValue : Value
{

    protected EvaluatedTypeImpl(Option<Identifier> alias, string representationNoAlias)
    {
        Representation = alias.Match(
            a => $"({a.Name}) {representationNoAlias}",
            () => representationNoAlias);
        Alias = alias;
        RepresentationNoAlias = representationNoAlias;
    }
    public Option<Identifier> Alias { get; }

    public string Representation { get; }
    protected string RepresentationNoAlias { get; }

    public virtual bool IsConvertibleTo(EvaluatedType other) => SemanticsEqual(other);

    public virtual bool IsAssignableTo(EvaluatedType other) => IsConvertibleTo(other) || other is EvaluatedTypeImpl<TValue> o && o.IsConvertibleFrom(this);

    /// <summary>
    /// Is another type implicitly convertible to this type?
    /// </summary>
    /// <param name="other">Another type to compare with the current type.</param>
    /// <returns><paramref name="other"/> is implicitly convertible to this type.</returns>
    protected virtual bool IsConvertibleFrom(EvaluatedTypeImpl<TValue> other) => false;

    public abstract bool SemanticsEqual(EvaluatedType other);

    public abstract EvaluatedType ToAliasReference(Identifier alias);

    public override string ToString() => Representation;

    public abstract TValue DefaultValue { get; }
    public abstract TValue RuntimeValue { get; }
    public abstract TValue GarbageValue { get; }
    public abstract TValue InvalidValue { get; }
    Value EvaluatedType.RuntimeValue => RuntimeValue;
    Value EvaluatedType.GarbageValue => GarbageValue;
    Value EvaluatedType.DefaultValue => DefaultValue;
    Value EvaluatedType.InvalidValue => InvalidValue;
}

abstract class EvaluatedTypeImplNotInstantiable<TValue>(Option<Identifier> alias, string representationNoAlias, ValueStatus defaultValueStatus) : EvaluatedTypeImpl<TValue>(alias, representationNoAlias)
where TValue : class, Value
{
    TValue? _defaultValue;
    TValue? _runtimeValue;
    TValue? _garbageValue;
    TValue? _invalidValue;
    public override TValue DefaultValue => _defaultValue ??= CreateValue(defaultValueStatus);
    public override TValue RuntimeValue => _runtimeValue ??= CreateValue(ValueStatus.Runtime.Instance);
    public override TValue GarbageValue => _garbageValue ??= CreateValue(ValueStatus.Garbage.Instance);
    public override TValue InvalidValue => _invalidValue ??= CreateValue(ValueStatus.Invalid.Instance);

    protected abstract TValue CreateValue(ValueStatus status);
}

abstract class EvaluatedTypeImplInstantiable<TValue, TUnderlying>(Option<Identifier> alias, string representationNoAlias, ValueStatus<TUnderlying> defaultValueStatus) : EvaluatedTypeImpl<TValue>(alias, representationNoAlias), InstantiableType<TValue, TUnderlying>
where TValue : class, Value
where TUnderlying : notnull
{
    protected EvaluatedTypeImplInstantiable(Option<Identifier> alias, string representationNoAlias, TUnderlying defaultValue) : this(alias, representationNoAlias, ValueStatus.Comptime.Of(defaultValue))
    { }

    TValue? _defaultValue;
    TValue? _runtimeValue;
    TValue? _garbageValue;
    TValue? _invalidValue;

    public override TValue DefaultValue => _defaultValue ??= CreateValue(defaultValueStatus);
    public override TValue RuntimeValue => _runtimeValue ??= CreateValue(ValueStatus.Runtime<TUnderlying>.Instance);
    public override TValue GarbageValue => _garbageValue ??= CreateValue(ValueStatus.Garbage<TUnderlying>.Instance);
    public override TValue InvalidValue => _invalidValue ??= CreateValue(ValueStatus.Invalid<TUnderlying>.Instance);

    public TValue Instantiate(TUnderlying value) => CreateValue(ValueStatus.Comptime.Of(value));
    protected abstract TValue CreateValue(ValueStatus<TUnderlying> status);
}
