using Scover.Psdc.Messages;
using Scover.Psdc.Pseudocode;

using static Scover.Psdc.Parsing.Node;
using static Scover.Psdc.Parsing.Node.UnaryOperator;
using static Scover.Psdc.Parsing.Node.BinaryOperator;

namespace Scover.Psdc.StaticAnalysis;

static class ConstantFolding
{
    static OperationResult<UnaryOperationMessage> Operate<TResultValue, TOperand, TResult>(
        InstantiableType<TResultValue, TResult> type,
        ValueStatus<TOperand> value,
        Func<TOperand, TResult> operation) where TResultValue : Value
     => OperationResult.OkUnary(
            value.ComptimeValue.Map(v => type.Instanciate(operation(v))).ValueOr(type.RuntimeValue));

    static OperationResult<BinaryOperationMessage> Operate<TResultValue, TLeft, TRight, TResult>(
        InstantiableType<TResultValue, TResult> type,
        ValueStatus<TLeft> left,
        ValueStatus<TRight> right,
        Func<TLeft, TRight, TResult> operation) where TResultValue : Value
     => OperationResult.OkBinary(
            left.ComptimeValue.Zip(right.ComptimeValue).Map((l, r) => type.Instanciate(operation(l, r))).ValueOr(type.RuntimeValue));

    static TResult Operate<TLeft, TRight, TResult>(
        ValueStatus<TLeft> left,
        ValueStatus<TRight> right,
        Func<Option<TLeft>, Option<TRight>, TResult> operation)
     => operation(left.ComptimeValue, right.ComptimeValue);

    internal static OperationResult<UnaryOperationMessage> EvaluateOperation(this StaticAnalyzer sa, Scope scope, UnaryOperator op, Value operand) => (op, operand) switch {
        (_, UnknownValue) => OperationResult.OkUnary(operand),

        // Arithmetic
        (Minus, IntegerValue x) => Operate(x.Type, x.Status, x => -x),
        (Minus, RealValue x) => Operate(x.Type, x.Status, x => -x),
        (Plus, IntegerValue x) => OperationResult.OkUnary(x),
        (Plus, RealValue x) => OperationResult.OkUnary(x),

        // Bitwise
        (Not, IntegerValue x) => Operate(x.Type, x.Status, x => ~x),

        // Logical
        (Not, BooleanValue x) => Operate(x.Type, x.Status, x => !x),

        // Other
        (Cast c, _) => sa.EvaluateCast(scope, c, operand),

        _ => (UnaryOperationMessage)Message.ErrorUnsupportedOperation,
    };

    static OperationResult<UnaryOperationMessage> EvaluateCast(this StaticAnalyzer sa, Scope scope, Cast cast, Value operand)
    {
        EvaluatedType targetType = sa.EvaluateType(scope, cast.Target);
        // Implicit conversions
        if (operand.Type.IsConvertibleTo(targetType)) {
            return OperationResult.OkUnary(operand, (opUn, operandType) => Message.HintRedundantCast(opUn.Location, operandType, targetType));
        }
        // Explicit conversions
        return (operand, targetType) switch {
            (BooleanValue bv, IntegerType it) => Operate(it, bv.Status, b => b ? 1 : 0),
            (CharacterValue, IntegerType it) => OperationResult.OkUnary(it.RuntimeValue), // runtime since target language encoding is unknown
            (IntegerValue iv, BooleanType bt) => Operate(bt, iv.Status, i => i != 0),
            (IntegerValue, CharacterType ct) => OperationResult.OkUnary(ct.RuntimeValue), // runtime since target language encoding is unknown
            (RealValue rv, IntegerType it) => Operate(it, rv.Status, r => (int)r),
            _ => (UnaryOperationMessage)((opUn, operandType) => Message.ErrorInvalidCast(opUn.Location, operandType, targetType)),
        };
    }

    internal static OperationResult<BinaryOperationMessage> EvaluateOperation(this BinaryOperator op, Value left, Value right) => (op, left, right) switch {
        // Propagate unknown types
        (_, UnknownValue, _) => OperationResult.OkBinary(left),
        (_, _, UnknownValue) => OperationResult.OkBinary(right),

        // Equality
        (Equal, BooleanValue l, BooleanValue r) => Operate(BooleanType.Instance, l.Status, r.Status, (l, r) => l == r),
        (Equal, CharacterValue l, CharacterValue r) => Operate(BooleanType.Instance, l.Status, r.Status, (l, r) => l == r),
        (Equal, StringValue l, StringValue r) => Operate(BooleanType.Instance, l.Status, r.Status, (l, r) => l == r),
        (Equal, IntegerValue l, IntegerValue r) => Operate(BooleanType.Instance, l.Status, r.Status, (l, r) => l == r),
        (Equal, RealValue l, RealValue r) => Operate(BooleanType.Instance, l.Status, r.Status, (l, r) => l == r)
            .WithMessages((opBin, _, _) => Message.WarningFloatingPointEquality(opBin.Location)),

        // Unequality
        (NotEqual, BooleanValue l, BooleanValue r) => Operate(BooleanType.Instance, l.Status, r.Status, (l, r) => l != r),
        (NotEqual, CharacterValue l, CharacterValue r) => Operate(BooleanType.Instance, l.Status, r.Status, (l, r) => l != r),
        (NotEqual, StringValue l, StringValue r) => Operate(BooleanType.Instance, l.Status, r.Status, (l, r) => l != r),
        (NotEqual, IntegerValue l, IntegerValue r) => Operate(BooleanType.Instance, l.Status, r.Status, (l, r) => l != r),
        (NotEqual, RealValue l, RealValue r) => Operate(BooleanType.Instance, l.Status, r.Status, (l, r) => l != r)
            .WithMessages((opBin, _, _) => Message.WarningFloatingPointEquality(opBin.Location)),

        // Comparison
        (GreaterThan, IntegerValue l, IntegerValue r) => Operate(BooleanType.Instance, l.Status, r.Status, (l, r) => l > r),
        (GreaterThan, RealValue l, RealValue r) => Operate(BooleanType.Instance, l.Status, r.Status, (l, r) => l > r),
        (GreaterThan, StringValue l, StringValue r) => Operate(BooleanType.Instance, l.Status, r.Status, (l, r) => string.CompareOrdinal(l, r) > 0),
        (GreaterThanOrEqual, IntegerValue l, IntegerValue r) => Operate(BooleanType.Instance, l.Status, r.Status, (l, r) => l >= r),
        (GreaterThanOrEqual, RealValue l, RealValue r) => Operate(BooleanType.Instance, l.Status, r.Status, (l, r) => l >= r),
        (GreaterThanOrEqual, StringValue l, StringValue r) => Operate(BooleanType.Instance, l.Status, r.Status, (l, r) => string.CompareOrdinal(l, r) >= 0),
        (LessThan, IntegerValue l, IntegerValue r) => Operate(BooleanType.Instance, l.Status, r.Status, (l, r) => l < r),
        (LessThan, RealValue l, RealValue r) => Operate(BooleanType.Instance, l.Status, r.Status, (l, r) => l < r),
        (LessThan, StringValue l, StringValue r) => Operate(BooleanType.Instance, l.Status, r.Status, (l, r) => string.CompareOrdinal(l, r) < 0),
        (LessThanOrEqual, IntegerValue l, IntegerValue r) => Operate(BooleanType.Instance, l.Status, r.Status, (l, r) => l <= r),
        (LessThanOrEqual, RealValue l, RealValue r) => Operate(BooleanType.Instance, l.Status, r.Status, (l, r) => l <= r),
        (LessThanOrEqual, StringValue l, StringValue r) => Operate(BooleanType.Instance, l.Status, r.Status, (l, r) => string.CompareOrdinal(l, r) <= 0),

        // Arithmetic
        (Add, IntegerValue l, IntegerValue r) => Operate(IntegerType.Instance, l.Status, r.Status, (l, r) => l + r),
        (Add, RealValue l, RealValue r) => Operate(RealType.Instance, l.Status, r.Status, (l, r) => l + r),
        (Divide, IntegerValue l, IntegerValue r) => Operate(l.Status, r.Status,
            (l, r) => l.Zip(r).Map((l, r) => r == 0
                ? OperationResult.OkBinary(IntegerType.Instance.RuntimeValue)
                    .WithMessages((opBin, _, _) => Message.WarningDivisionByZero(opBin.Location))
                : OperationResult.OkBinary(IntegerType.Instance.Instanciate(l / r)))
            .ValueOr(OperationResult.OkBinary(IntegerType.Instance.RuntimeValue))),
        (Divide, RealValue l, RealValue r) => Operate(RealType.Instance, l.Status, r.Status, (l, r) => l / r),
        (Mod, IntegerValue l, IntegerValue r) => Operate(IntegerType.Instance, l.Status, r.Status, (l, r) => l % r),
        (Mod, RealValue l, RealValue r) => Operate(RealType.Instance, l.Status, r.Status, (l, r) => l % r),
        (Multiply, IntegerValue l, IntegerValue r) => Operate(IntegerType.Instance, l.Status, r.Status, (l, r) => l * r),
        (Multiply, RealValue l, RealValue r) => Operate(RealType.Instance, l.Status, r.Status, (l, r) => l * r),
        (Subtract, IntegerValue l, IntegerValue r) => Operate(IntegerType.Instance, l.Status, r.Status, (l, r) => l - r),
        (Subtract, RealValue l, RealValue r) => Operate(RealType.Instance, l.Status, r.Status, (l, r) => l - r),

        // Bitwise
        (And, IntegerValue l, IntegerValue r) => Operate(IntegerType.Instance, l.Status, r.Status, (l, r) => l & r),
        (Or, IntegerValue l, IntegerValue r) => Operate(IntegerType.Instance, l.Status, r.Status, (l, r) => l | r),
        (Xor, IntegerValue l, IntegerValue r) => Operate(IntegerType.Instance, l.Status, r.Status, (l, r) => l ^ r),

        // Logical
        (And, BooleanValue l, BooleanValue r) => Operate(BooleanType.Instance, l.Status, r.Status, (l, r) => l && r),
        (Or, BooleanValue l, BooleanValue r) => Operate(BooleanType.Instance, l.Status, r.Status, (l, r) => l || r),
        (Xor, BooleanValue l, BooleanValue r) => Operate(BooleanType.Instance, l.Status, r.Status, (l, r) => l ^ r),

        _ => (BinaryOperationMessage)Message.ErrorUnsupportedOperation,
    };
}
