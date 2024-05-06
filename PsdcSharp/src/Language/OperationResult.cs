namespace Scover.Psdc.Language;

static class OperationResult
{
    private static readonly Value failureVal = Value.Of(EvaluatedType.Unknown.Inferred);

    public static OperationResult<Value> Fail(OperationError fatalError)
     => new(failureVal, fatalError.Yield());

    public static OperationResult<TValue> Ok<TValue>(TValue value) where TValue : Value
     => new(value, []);

    public static OperationResult<TValue> Ok<TValue>(TValue value, params OperationError[] errors) where TValue : Value
     => new(value, errors);
}

readonly struct OperationResult<TValue> where TValue : Value
{
    private static readonly Value failureVal = Value.Of(EvaluatedType.Unknown.Inferred);

    public OperationResult(TValue value, IEnumerable<OperationError> errors) : this((Value)value, errors) {}    
    private OperationResult(Value value, IEnumerable<OperationError> errors)
     => (Value, Errors) = (value, errors);

    public Value Value { get; }
    public IEnumerable<OperationError> Errors { get; }

    public static implicit operator OperationResult<TValue>(TValue val) => new(val, []);
    public static implicit operator OperationResult<TValue>(OperationError error) => new(failureVal, error.Yield());
    public static implicit operator OperationResult<Value>(OperationResult<TValue> other) => new(other.Value, other.Errors);
}
