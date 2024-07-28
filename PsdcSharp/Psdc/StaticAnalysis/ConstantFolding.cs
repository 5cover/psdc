using Scover.Psdc.Language;
using Scover.Psdc.Messages;
using static Scover.Psdc.Parsing.Node;
using static Scover.Psdc.Language.UnaryOperator;
using static Scover.Psdc.Language.BinaryOperator;

namespace Scover.Psdc.StaticAnalysis;

static class ConstantFolding
{
    static OperationResult<TResultValue> Operate<TResultValue, TOperand, TResult>(
        InstantiableType<TResultValue, TResult> type,
        Value<TOperand> value,
        Func<TOperand, TResult> operation) where TResultValue : Value
     => OperationResult.Ok(value.Value.Comptime.Map(v => type.Instantiate(operation(v))).ValueOr(type.RuntimeValue));

    static OperationResult<TResultValue> Operate<TResultValue, TLeft, TRight, TResult>(
        InstantiableType<TResultValue, TResult> type,
        Value<TLeft> left,
        Value<TRight> right,
        Func<TLeft, TRight, TResult> operation) where TResultValue : Value
     => OperationResult.Ok(left.Value.Comptime.Combine(right.Value.Comptime).Map((l, r) => type.Instantiate(operation(l, r))).ValueOr(type.RuntimeValue));

    static TResult Operate<TLeft, TRight, TResult>(
        Value<TLeft> left,
        Value<TRight> right,
        Func<Option<TLeft>, Option<TRight>, TResult> operation)
     => operation(left.Value.Comptime, right.Value.Comptime);

    internal static OperationResult<Value> EvaluateOperation(this UnaryOperator op, Value operand) => (op, operand) switch {
        (_, UnknownValue) => OperationResult.Ok(operand),
        (Minus, IntegerValue x) => Operate(x.Type, x, (int x) => -x),
        (Minus, RealValue x) => Operate(x.Type, x, x => -x),
        (Not, BooleanValue x) => Operate(x.Type, x, x => !x),
        (Plus, IntegerValue x) => x,
        (Plus, RealValue x) => OperationResult.Ok(x),

        _ => OperationMessage.ErrorUnsupportedOperator,
    };

    internal static OperationResult<Value> EvaluateOperation(this BinaryOperator op, Value left, Value right) => (op, left, right) switch {
        // Propagate unknown types
        (_, UnknownValue, _) => OperationResult.Ok(left),
        (_, _, UnknownValue) => OperationResult.Ok(right),

        // Equality
        (Equal, BooleanValue l, BooleanValue r) => Operate(BooleanType.Instance, l, r, (l, r) => l == r),
        (Equal, CharacterValue l, CharacterValue r) => Operate(BooleanType.Instance, l, r, (l, r) => l == r),
        (Equal, StringValue l, StringValue r) => Operate(BooleanType.Instance, l, r, (l, r) => l == r),
        (Equal, IntegerValue l, IntegerValue r) => Operate(BooleanType.Instance, l, r, (int l, int r) => l == r),
        (Equal, RealValue l, RealValue r) => Operate(BooleanType.Instance, l, r, (l, r) => l == r).WithMessages(OperationMessage.WarningFloatingPointEquality),

        // Unequality
        (NotEqual, BooleanValue l, BooleanValue r) => Operate(BooleanType.Instance, l, r, (l, r) => l != r),
        (NotEqual, CharacterValue l, CharacterValue r) => Operate(BooleanType.Instance, l, r, (l, r) => l != r),
        (NotEqual, StringValue l, StringValue r) => Operate(BooleanType.Instance, l, r, (l, r) => l != r),
        (NotEqual, IntegerValue l, IntegerValue r) => Operate(BooleanType.Instance, l, r, (int l, int r) => l != r),
        (NotEqual, RealValue l, RealValue r) => Operate(BooleanType.Instance, l, r, (l, r) => l != r).WithMessages(OperationMessage.WarningFloatingPointEquality),

        // Comparison
        (GreaterThan, IntegerValue l, IntegerValue r) => Operate(BooleanType.Instance, l, r, (int l, int r) => l > r),
        (GreaterThan, RealValue l, RealValue r) => Operate(BooleanType.Instance, l, r, (l, r) => l > r),
        (GreaterThan, StringValue l, StringValue r) => Operate(BooleanType.Instance, l, r, (l, r) => string.CompareOrdinal(l, r) > 0),
        (GreaterThanOrEqual, IntegerValue l, IntegerValue r) => Operate(BooleanType.Instance, l, r, (int l, int r) => l >= r),
        (GreaterThanOrEqual, RealValue l, RealValue r) => Operate(BooleanType.Instance, l, r, (l, r) => l >= r),
        (GreaterThanOrEqual, StringValue l, StringValue r) => Operate(BooleanType.Instance, l, r, (l, r) => string.CompareOrdinal(l, r) >= 0),
        (LessThan, IntegerValue l, IntegerValue r) => Operate(BooleanType.Instance, l, r, (int l, int r) => l < r),
        (LessThan, RealValue l, RealValue r) => Operate(BooleanType.Instance, l, r, (l, r) => l < r),
        (LessThan, StringValue l, StringValue r) => Operate(BooleanType.Instance, l, r, (l, r) => string.CompareOrdinal(l, r) < 0),
        (LessThanOrEqual, IntegerValue l, IntegerValue r) => Operate(BooleanType.Instance, l, r, (int l, int r) => l <= r),
        (LessThanOrEqual, RealValue l, RealValue r) => Operate(BooleanType.Instance, l, r, (l, r) => l <= r),
        (LessThanOrEqual, StringValue l, StringValue r) => Operate(BooleanType.Instance, l, r, (l, r) => string.CompareOrdinal(l, r) <= 0),

        // Arithmetic
        (Add, IntegerValue l, IntegerValue r) => Operate(IntegerType.Instance, l, r, (int l, int r) => l + r),
        (Add, RealValue l, RealValue r) => Operate(RealType.Instance, l, r, (l, r) => l + r),
        (Divide, IntegerValue l, IntegerValue r) => Operate(l, r,
            (Option<int> l, Option<int> r) => l.Combine(r).Map((l, r) => r == 0
                ? OperationResult.Ok(IntegerType.Instance.RuntimeValue).WithMessages(OperationMessage.WarningDivisionByZero)
                : OperationResult.Ok(IntegerType.Instance.Instantiate(l / r)))
            .ValueOr(IntegerType.Instance.RuntimeValue)),
        (Divide, RealValue l, RealValue r) => Operate(RealType.Instance, l, r, (l, r) => l / r),
        (Mod, IntegerValue l, IntegerValue r) => Operate(IntegerType.Instance, l, r, (int l, int r) => l % r),
        (Mod, RealValue l, RealValue r) => Operate(RealType.Instance, l, r, (l, r) => l % r),
        (Multiply, IntegerValue l, IntegerValue r) => Operate(IntegerType.Instance, l, r, (int l, int r) => l * r),
        (Multiply, RealValue l, RealValue r) => Operate(RealType.Instance, l, r, (l, r) => l * r),
        (Subtract, IntegerValue l, IntegerValue r) => Operate(IntegerType.Instance, l, r, (int l, int r) => l - r),
        (Subtract, RealValue l, RealValue r) => Operate(RealType.Instance, l, r, (l, r) => l - r),

        // Boolean

        (And, BooleanValue l, BooleanValue r) => Operate(BooleanType.Instance, l, r, (l, r) => l && r),
        (Or, BooleanValue l, BooleanValue r) => Operate(BooleanType.Instance, l, r, (l, r) => l || r),
        (Xor, BooleanValue l, BooleanValue r) => Operate(BooleanType.Instance, l, r, (l, r) => l ^ r),

        _ => OperationMessage.ErrorUnsupportedOperator,
    };

    internal static Message GetOperationMessage(this OperationMessage error, Expression.UnaryOperation opUn, EvaluatedType operandType) => error switch {
        OperationMessage.ErrorUnsupportedOperator => Message.ErrorUnsupportedOperation(opUn, operandType),
        _ => throw error.ToUnmatchedException(),
    };

    internal static Message GetOperationMessage(this OperationMessage error, Expression.BinaryOperation opBin, EvaluatedType leftType, EvaluatedType rightType) => error switch {
        OperationMessage.ErrorUnsupportedOperator => Message.ErrorUnsupportedOperation(opBin, leftType, rightType),
        OperationMessage.WarningDivisionByZero => Message.WarningDivisionByZero(opBin.SourceTokens),
        OperationMessage.WarningFloatingPointEquality => Message.WarningFloatingPointEquality(opBin.SourceTokens),
        _ => throw error.ToUnmatchedException(),
    };
}
