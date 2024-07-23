using Scover.Psdc.Language;
using Scover.Psdc.Messages;
using static Scover.Psdc.Parsing.Node;
using static Scover.Psdc.Language.UnaryOperator;
using static Scover.Psdc.Language.BinaryOperator;
using static Scover.Psdc.Language.Value;
using Boolean = Scover.Psdc.Language.Value.Boolean;
using String = Scover.Psdc.Language.Value.String;

namespace Scover.Psdc.StaticAnalysis;

partial class AstExtensions
{
    internal static OperationResult<Value> EvaluateOperation(this UnaryOperator op, Value operand) => (op, operand) switch {
        (_, Unknown u) => u,
        (Minus, Integer x) => x.Operate(x => -x),
        (Minus, Real x) => x.Operate(x => -x),
        (Not, Boolean x) => x.Operate(x => !x),
        (Plus, Real x) => x,

        _ => OperationMessage.ErrorUnsupportedOperator,
    };

    internal static OperationResult<Value> EvaluateOperation(this BinaryOperator op, Value left, Value right) => (op, left, right) switch {
        // Propagate unknown types
        (_, Unknown u, _) => u,
        (_, _, Unknown u) => u,

        // Equality
        (Equal, Boolean l, Boolean r) => l.OperateWith(r, (l, r) => l == r),
        (Equal, Character l, Character r) => l.OperateWith(r, (l, r) => new Boolean(l == r)),
        (Equal, String l, String r) => l.OperateWith(r, (l, r) => new Boolean(l == r)),
        (Equal, Integer l, Integer r) => l.OperateWith(r, (l, r) => new Boolean(l == r)),
        (Equal, Real l, Real r) => l.OperateWith(r, (l, r) => OperationResult.Ok(new Boolean(l == r), OperationMessage.WarningFloatingPointEquality)),

        // Unequality
        (NotEqual, Boolean l, Boolean r) => l.OperateWith(r, (l, r) => l != r),
        (NotEqual, Character l, Character r) => l.OperateWith(r, (l, r) => new Boolean(l != r)),
        (NotEqual, String l, String r) => l.OperateWith(r, (l, r) => new Boolean(l != r)),
        (NotEqual, Integer l, Integer r) => l.OperateWith(r, (l, r) => new Boolean(l != r)),
        (NotEqual, Real l, Real r) => l.OperateWith(r, (l, r) => OperationResult.Ok(new Boolean(l != r), OperationMessage.WarningFloatingPointEquality)),

        // Comparison
        (GreaterThan, Integer l, Integer r) => l.OperateWith(r, (l, r) => new Boolean(l > r)),
        (GreaterThan, Real l, Real r) => l.OperateWith(r, (l, r) => new Boolean(l > r)),
        (GreaterThanOrEqual, Integer l, Integer r) => l.OperateWith(r, (l, r) => new Boolean(l >= r)),
        (GreaterThanOrEqual, Real l, Real r) => l.OperateWith(r, (l, r) => new Boolean(l >= r)),
        (LessThan, Integer l, Integer r) => l.OperateWith(r, (l, r) => new Boolean(l < r)),
        (LessThan, Real l, Real r) => l.OperateWith(r, (l, r) => new Boolean(l < r)),
        (LessThanOrEqual, Integer l, Integer r) => l.OperateWith(r, (l, r) => new Boolean(l <= r)),
        (LessThanOrEqual, Real l, Real r) => l.OperateWith(r, (l, r) => new Boolean(l <= r)),

        // Arithmetic
        (Add, Integer l, Integer r) => l.OperateWith(r, (l, r) => l + r),
        (Add, Real l, Real r) => l.OperateWith(r, (l, r) => l + r),
        (Divide, Integer l, Integer r) => l.OperateWith(r, (l, r) => r == 0 ? OperationMessage.WarningDivisionByZero : new Integer(l / r)),
        (Divide, Real l, Real r) => l.OperateWith(r, (l, r) => l / r),
        (Mod, Integer l, Integer r) => l.OperateWith(r, (l, r) => l % r),
        (Mod, Real l, Real r) => l.OperateWith(r, (l, r) => l % r),
        (Multiply, Integer l, Integer r) => l.OperateWith(r, (l, r) => l * r),
        (Multiply, Real l, Real r) => l.OperateWith(r, (l, r) => l * r),
        (Subtract, Integer l, Integer r) => l.OperateWith(r, (l, r) => l - r),
        (Subtract, Real l, Real r) => l.OperateWith(r, (l, r) => l - r),

        // Boolean

        (And, Boolean l, Boolean r) => l.OperateWith(r, (l, r) => l && r),
        (Or, Boolean l, Boolean r) => l.OperateWith(r, (l, r) => l || r),
        (Xor, Boolean l, Boolean r) => l.OperateWith(r, (l, r) => l ^ r),

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
