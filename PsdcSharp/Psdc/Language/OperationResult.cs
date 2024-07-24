namespace Scover.Psdc.Language;

static class OperationResult
{
    public static OperationResult<TValue> Ok<TValue>(TValue value) where TValue : Value
     => new(value, []);

    public static OperationResult<TValue> Ok<TValue>(TValue value, params OperationMessage[] errors) where TValue : Value
     => new(value, errors);
}

readonly struct OperationResult<TValue> where TValue : Value
{
    public OperationResult(TValue value, IEnumerable<OperationMessage> errors) : this((Value)value, errors) { }
    OperationResult(Value value, IEnumerable<OperationMessage> errors)
     => (Value, Errors) = (value, errors);

    public Value Value { get; }
    public IEnumerable<OperationMessage> Errors { get; }

    public static implicit operator OperationResult<TValue>(TValue val) => new(val, []);
    public static implicit operator OperationResult<TValue>(OperationMessage error) => new(EvaluatedType.Unknown.Inferred.UnknownValue, error.Yield());
    public static implicit operator OperationResult<Value>(OperationResult<TValue> other) => new(other.Value, other.Errors);
}
