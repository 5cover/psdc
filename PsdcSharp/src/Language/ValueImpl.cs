namespace Scover.Psdc.Language;

abstract class ValueImpl<TSelf, TVal> : Value<TSelf, TVal>
    where TSelf : Value<TSelf, TVal>, Value<TSelf>
{
    protected ValueImpl(TVal value) => Value = value.Some();
    protected ValueImpl() => Value = Option.None<TVal>();

    public abstract EvaluatedType Type { get; }
    Option<EvaluatedType> Value.Type => Type.Some();

    public Option<TVal> Value { get; }
    public bool IsConstant => Value.HasValue;

    public bool SemanticsEqual(Value other) => other is TSelf o
        && o.Value == Value;
    protected abstract TSelf Create(TVal val);

    // Binary
    public OperationResult<TResult> OperateWith<TResult>(TSelf other,
        Func<TVal, TVal, OperationResult<TResult>> transform)
        where TResult : Value<TResult>
     => Value.Combine(other.Value).Map(transform).ValueOr(OperationResult.Ok(TResult.NoValue));

    // Binary error-less
    public TResult OperateWith<TResult>(TSelf other,
        Func<TVal, TVal, TResult> transform)
        where TResult : Value<TResult>
     => Value.Combine(other.Value).Map(transform).ValueOr(TResult.NoValue);

    // Binary internal
    public OperationResult<TSelf> OperateWith(TSelf other,
        Func<TVal, TVal, OperationResult<TSelf>> transform)
     => OperateWith<TSelf>(other, transform);

    // Binary internal error-less
    public TSelf OperateWith(TSelf other,
       Func<TVal, TVal, TVal> transform)
     => Value.Combine(other.Value).Map(transform).Map(Create).ValueOr(TSelf.NoValue);

    // Unary
    public OperationResult<TResult> Operate<TResult>(
        Func<TVal, OperationResult<TResult>> transform)
        where TResult : Value<TResult>
     => Value.Map(transform).ValueOr(OperationResult.Ok(TResult.NoValue));

    // Unary error-less
    public TResult Operate<TResult>(
        Func<TVal, TResult> transform)
        where TResult : Value<TResult>
     => Value.Map(transform).ValueOr(TResult.NoValue);

    // Unary internal
    public OperationResult<TSelf> Operate(
        Func<TVal, OperationResult<TSelf>> transform)
     => Operate<TSelf>(transform);

    // Unary internal error-less
    public TSelf Operate(
        Func<TVal, TVal> transform)
     => Value.Map(transform).Map(Create).ValueOr(TSelf.NoValue);
}
