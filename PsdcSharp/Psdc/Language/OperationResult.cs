namespace Scover.Psdc.Language;

static class OperationResult
{
    public static OperationResult<TValue> Ok<TValue>(TValue value) where TValue : Value
     => new(value, []);

    public static OperationResult<TValue> Ok<TValue>(TValue value, params OperationError[] errors) where TValue : Value
     => new(value, errors);
}

readonly struct OperationResult<TValue> where TValue : Value
{
    public OperationResult(TValue value, IEnumerable<OperationError> errors) : this((Value)value, errors) { }
    OperationResult(Value value, IEnumerable<OperationError> errors)
     => (Value, Errors) = (value, errors);

    public Value Value { get; }
    public IEnumerable<OperationError> Errors { get; }

    public static implicit operator OperationResult<TValue>(TValue val) => new(val, []);
    public static implicit operator OperationResult<TValue>(OperationError error) => new(Value.UnknownInferred, error.Yield());
    public static implicit operator OperationResult<Value>(OperationResult<TValue> other) => new(other.Value, other.Errors);
}
