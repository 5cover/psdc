using Scover.Psdc.Language;
using Scover.Psdc.Messages;
using static Scover.Psdc.Parsing.Node;
using static Scover.Psdc.Language.UnaryOperator;
using static Scover.Psdc.Language.EvaluatedType;
using static Scover.Psdc.Language.BinaryOperator;
using Boolean = Scover.Psdc.Language.EvaluatedType.Boolean;
using String = Scover.Psdc.Language.EvaluatedType.String;

namespace Scover.Psdc.StaticAnalysis;

partial class AstExtensions
{
    static TValue Operate<TValue, TUnderlying, TUnderlyingResult>(
        InstantiableType<TValue, TUnderlyingResult> type,
        Value<TUnderlying> value,
        Func<TUnderlying, TUnderlyingResult> operation) where TValue : Value
     => type.CreateValue(value.UnderlyingValue.Map(operation));

    static TValue Operate<TValue, TUnderlyingLeft, TUnderlyingRight, TUnderlyingResult>(
        InstantiableType<TValue, TUnderlyingResult> type,
        Value<TUnderlyingLeft> left,
        Value<TUnderlyingRight> right,
        Func<TUnderlyingLeft, TUnderlyingRight, TUnderlyingResult> operation) where TValue : Value
     => type.CreateValue(left.UnderlyingValue.Combine(right.UnderlyingValue).Map(operation));

    static TResult Operate<TUnderlyingLeft, TUnderlyingRight, TResult>(
        Value<TUnderlyingLeft> left,
        Value<TUnderlyingRight> right,
        Func<Option<TUnderlyingLeft>, Option<TUnderlyingRight>, TResult> operation)
     => operation(left.UnderlyingValue, right.UnderlyingValue);

    internal static OperationResult<Value> EvaluateOperation(this UnaryOperator op, Value operand) => (op, operand) switch {
        (_, Unknown.Value) => OperationResult.Ok(operand),
        (Minus, Integer.Value x) => Operate(x.Type, x, (int x) => -x),
        (Minus, Real.Value x) => Operate(x.Type, x, x => -x),
        (Not, Boolean.Value x) => Operate(x.Type, x, x => !x),
        (Plus, Integer.Value x) => x,
        (Plus, Real.Value x) => x,

        _ => OperationMessage.ErrorUnsupportedOperator,
    };

    internal static OperationResult<Value> EvaluateOperation(this BinaryOperator op, Value left, Value right) => (op, left, right) switch {
        // Propagate unknown types
        (_, Unknown.Value, _) => OperationResult.Ok(left),
        (_, _, Unknown.Value) => OperationResult.Ok(right),

        // Equality
        (Equal, Boolean.Value l, Boolean.Value r) => Operate(Boolean.Instance, l, r, (l, r) => l == r),
        (Equal, Character.Value l, Character.Value r) => Operate(Boolean.Instance, l, r, (l, r) => l == r),
        (Equal, String.Value l, String.Value r) => Operate(Boolean.Instance, l, r, (l, r) => l == r),
        (Equal, Integer.Value l, Integer.Value r) => Operate(Boolean.Instance, l, r, (int l, int r) => l == r),
        (Equal, Real.Value l, Real.Value r) => OperationResult.Ok(Operate(Boolean.Instance, l, r, (l, r) => l == r), OperationMessage.WarningFloatingPointEquality),

        // Unequality
        (NotEqual, Boolean.Value l, Boolean.Value r) => Operate(Boolean.Instance, l, r, (l, r) => l != r),
        (NotEqual, Character.Value l, Character.Value r) => Operate(Boolean.Instance, l, r, (l, r) => l != r),
        (NotEqual, String.Value l, String.Value r) => Operate(Boolean.Instance, l, r, (l, r) => l != r),
        (NotEqual, Integer.Value l, Integer.Value r) => Operate(Boolean.Instance, l, r, (int l, int r) => l != r),
        (NotEqual, Real.Value l, Real.Value r) => OperationResult.Ok(Operate(Boolean.Instance, l, r, (l, r) => l != r),
            OperationMessage.WarningFloatingPointEquality),

        // Comparison
        (GreaterThan, Integer.Value l, Integer.Value r) => Operate(Boolean.Instance, l, r, (int l, int r) => l > r),
        (GreaterThan, Real.Value l, Real.Value r) => Operate(Boolean.Instance, l, r, (l, r) => l > r),
        (GreaterThanOrEqual, Integer.Value l, Integer.Value r) => Operate(Boolean.Instance, l, r, (int l, int r) => l >= r),
        (GreaterThanOrEqual, Real.Value l, Real.Value r) => Operate(Boolean.Instance, l, r, (l, r) => l >= r),
        (LessThan, Integer.Value l, Integer.Value r) => Operate(Boolean.Instance, l, r, (int l, int r) => l < r),
        (LessThan, Real.Value l, Real.Value r) => Operate(Boolean.Instance, l, r, (l, r) => l < r),
        (LessThanOrEqual, Integer.Value l, Integer.Value r) => Operate(Boolean.Instance, l, r, (int l, int r) => l <= r),
        (LessThanOrEqual, Real.Value l, Real.Value r) => Operate(Boolean.Instance, l, r, (l, r) => l <= r),

        // Arithmetic
        (Add, Integer.Value l, Integer.Value r) => Operate(Integer.Instance, l, r, (int l, int r) => l + r),
        (Add, Real.Value l, Real.Value r) => Operate(Real.Instance, l, r, (l, r) => l + r),
        (Divide, Integer.Value l, Integer.Value r) => Operate(l, r,
            (Option<int> l, Option<int> r) => l.Combine(r).Map((l, r) => r == 0
                ? OperationResult.Ok(Integer.Instance.UnknownValue, OperationMessage.WarningDivisionByZero)
                : OperationResult.Ok(Integer.Instance.CreateValue((l / r).Some())))
            .ValueOr(Integer.Instance.UnknownValue)),
        (Divide, Real.Value l, Real.Value r) => Operate(Real.Instance, l, r, (l, r) => l / r),
        (Mod, Integer.Value l, Integer.Value r) => Operate(Integer.Instance, l, r, (int l, int r) => l % r),
        (Mod, Real.Value l, Real.Value r) => Operate(Real.Instance, l, r, (l, r) => l % r),
        (Multiply, Integer.Value l, Integer.Value r) => Operate(Integer.Instance, l, r, (int l, int r) => l * r),
        (Multiply, Real.Value l, Real.Value r) => Operate(Real.Instance, l, r, (l, r) => l * r),
        (Subtract, Integer.Value l, Integer.Value r) => Operate(Integer.Instance, l, r, (int l, int r) => l - r),
        (Subtract, Real.Value l, Real.Value r) => Operate(Real.Instance, l, r, (l, r) => l - r),

        // Boolean

        (And, Boolean.Value l, Boolean.Value r) => Operate(Boolean.Instance, l, r, (l, r) => l && r),
        (Or, Boolean.Value l, Boolean.Value r) => Operate(Boolean.Instance, l, r, (l, r) => l || r),
        (Xor, Boolean.Value l, Boolean.Value r) => Operate(Boolean.Instance, l, r, (l, r) => l ^ r),

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
