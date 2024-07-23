namespace Scover.Psdc.Language;

abstract class ValueImpl<TSelf, TUnderlying> : Value<TSelf, TUnderlying>
    where TSelf : Value<TSelf, TUnderlying>, Value<TSelf>
{
    protected ValueImpl(TUnderlying value) => Value = value.Some();
    protected ValueImpl() => Value = Option.None<TUnderlying>();

    public abstract EvaluatedType Type { get; }
    Option<EvaluatedType> Value.Type => Type.Some();

    public Option<TUnderlying> Value { get; }
    public bool IsConstant => Value.HasValue;

    public bool SemanticsEqual(Value other) => other is TSelf o
        && o.Value == Value;

    protected abstract TSelf Create(TUnderlying val);

    public OperationResult<TResult> OperateWith<TResult>(TSelf other,
        Func<TUnderlying, TUnderlying, OperationResult<TResult>> transform)
        where TResult : Value<TResult>
     => Value.Combine(other.Value).Map(transform).ValueOr(OperationResult.Ok(TResult.NoValue));

    public TResult OperateWith<TResult>(TSelf other,
        Func<TUnderlying, TUnderlying, TResult> transform)
        where TResult : Value<TResult>
     => Value.Combine(other.Value).Map(transform).ValueOr(TResult.NoValue);

    public OperationResult<TSelf> OperateWith(TSelf other,
        Func<TUnderlying, TUnderlying, OperationResult<TSelf>> transform)
     => OperateWith<TSelf>(other, transform);

    public TSelf OperateWith(TSelf other,
       Func<TUnderlying, TUnderlying, TUnderlying> transform)
     => Value.Combine(other.Value).Map(transform).Map(Create).ValueOr(TSelf.NoValue);

    public OperationResult<TResult> Operate<TResult>(
        Func<TUnderlying, OperationResult<TResult>> transform)
        where TResult : Value<TResult>
     => Value.Map(transform).ValueOr(OperationResult.Ok(TResult.NoValue));

    public TResult Operate<TResult>(
        Func<TUnderlying, TResult> transform)
        where TResult : Value<TResult>
     => Value.Map(transform).ValueOr(TResult.NoValue);

    public OperationResult<TSelf> Operate(
        Func<TUnderlying, OperationResult<TSelf>> transform)
     => Operate<TSelf>(transform);

    public TSelf Operate(
        Func<TUnderlying, TUnderlying> transform)
     => Value.Map(transform).Map(Create).ValueOr(TSelf.NoValue);
}
