using Scover.Psdc.Messages;
using static Scover.Psdc.Parsing.Node;

namespace Scover.Psdc.Pseudocode;

static class OperationResult
{
    public static OperationResult<BinaryOperationMessage> OkBinary(Value value, params BinaryOperationMessage[] messages) => new(value, messages);
    public static OperationResult<UnaryOperationMessage> OkUnary(Value value, params UnaryOperationMessage[] messages) => new(value, messages);
}

delegate Message UnaryOperationMessage(Expression.UnaryOperation opUn, EvaluatedType operandType);
delegate Message BinaryOperationMessage(Expression.BinaryOperation opBin, EvaluatedType leftType, EvaluatedType rightType);

readonly struct OperationResult<TMessage>
{
    public OperationResult(Value value, IEnumerable<TMessage> messages)
     => (Value, Messages) = (value, messages);

    public Value Value { get; }
    public IEnumerable<TMessage> Messages { get; }

    public OperationResult<TMessage> WithMessages(params TMessage[] messages)
     => new(Value, messages);

    public static implicit operator OperationResult<TMessage>(TMessage error) => new(UnknownType.Inferred.DefaultValue, error.Yield());
}
