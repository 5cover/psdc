namespace Scover.Psdc.Language;

static class OperationResult
{
    public static OperationResult<TValue> Ok<TValue>(TValue value) where TValue : Value
     => new(value, []);
}

readonly struct OperationResult<TValue> where TValue : Value
{
    public OperationResult(TValue value, IEnumerable<OperationMessage> messages) : this((Value)value, messages) { }
    OperationResult(Value value, IEnumerable<OperationMessage> messages)
     => (Value, Messages) = (value, messages);

    public Value Value { get; }
    public IEnumerable<OperationMessage> Messages { get; }

    public OperationResult<TValue> WithMessages(params OperationMessage[] messages)
     => new(Value, messages);

    public static implicit operator OperationResult<TValue>(TValue val) => new(val, []);
    public static implicit operator OperationResult<TValue>(OperationMessage error) => new(UnknownType.Inferred.DefaultValue, error.Yield());
    public static implicit operator OperationResult<Value>(OperationResult<TValue> other) => new(other.Value, other.Messages);
}
