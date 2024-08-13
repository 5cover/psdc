using Scover.Psdc.Parsing;

namespace Scover.Psdc.Language;

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

abstract class EvaluatedTypeImplNotInstantiable<TValue>(Identifier? alias, ValueStatus defaultValueStatus) : EvaluatedTypeImpl<TValue>(alias)
where TValue : class, Value
{
    private TValue? _defaultValue;
    private TValue? _runtimeValue;
    private TValue? _garbageValue;
    private TValue? _invalidValue;
    public override TValue DefaultValue => _defaultValue ??= CreateValue(defaultValueStatus);
    public override TValue RuntimeValue => _runtimeValue ??= CreateValue(ValueStatus.Runtime);
    public override TValue GarbageValue => _garbageValue ??= CreateValue(ValueStatus.Garbage);
    public override TValue InvalidValue => _invalidValue ??= CreateValue(ValueStatus.Invalid);

    protected abstract TValue CreateValue(ValueStatus status);
}

abstract class EvaluatedTypeImplInstantiable<TValue, TUnderlying>(Identifier? alias, ValueStatus<TUnderlying> defaultValueStatus) : EvaluatedTypeImpl<TValue>(alias), InstantiableType<TValue, TUnderlying>
where TValue : class, Value
{
    protected EvaluatedTypeImplInstantiable(Identifier? alias, TUnderlying defaultValue) : this(alias, Value.Comptime(defaultValue))
    { }

    private TValue? _defaultValue;
    private TValue? _runtimeValue;
    private TValue? _garbageValue;
    private TValue? _invalidValue;

    public override TValue DefaultValue => _defaultValue ??= CreateValue(defaultValueStatus);
    public override TValue RuntimeValue => _runtimeValue ??= CreateValue(Value.Runtime<TUnderlying>());
    public override TValue GarbageValue => _garbageValue ??= CreateValue(Value.Garbage<TUnderlying>());
    public override TValue InvalidValue => _invalidValue ??= CreateValue(Value.Invalid<TUnderlying>());

    public TValue Instantiate(TUnderlying value) => CreateValue(Value.Comptime(value));
    protected abstract TValue CreateValue(ValueStatus<TUnderlying> status);
}
