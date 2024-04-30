
using System.Numerics;
using Scover.Psdc.Parsing;

namespace Scover.Psdc.StaticAnalysis;

internal static partial class AstExtensions
{
    public static bool IsConstant(this Node.Expression expression, ReadOnlyScope scope) => expression switch {
        Node.Expression.Bracketed b => b.Expression.IsConstant(scope),
        Node.Expression.Literal => true,
        Node.Expression.Lvalue.ArraySubscript arrSub => arrSub.Array.IsConstant(scope) && arrSub.Indexes.All(i => i.IsConstant(scope)),
        Node.Expression.Lvalue.Bracketed b => b.Lvalue.IsConstant(scope),
        Node.Expression.Lvalue.ComponentAccess compAccess => compAccess.Structure.IsConstant(scope),
        Node.Expression.Lvalue.VariableReference varRef => scope.GetSymbol<Symbol.Constant>(varRef.Name).HasValue,
        Node.Expression.OperationBinary ob => ob.Operand1.IsConstant(scope) && ob.Operand2.IsConstant(scope),
        Node.Expression.OperationUnary ou => ou.Operand.IsConstant(scope),
        _ => throw expression.ToUnmatchedException(),
    };

    public static Option<TVal, Message> EvaluateConstantValue<TVal>(this Node.Expression expr, ReadOnlyScope scope, EvaluatedType expectedType)
    where TVal : ConstantValue
     => expr.EvaluateConstantValue(scope).FlatMap(val
         => val is TVal tval
            ? tval.Some<TVal, Message>()
            : Message.ErrorConstantExpressionWrongType(expr, expectedType, val.Type).None<TVal, Message>());

    public static Option<ConstantValue, Message> EvaluateConstantValue(this Node.Expression expr, ReadOnlyScope scope)
     => expr.EvaluateValue(scope).MapError(e => e.ValueOr(Message.ErrorConstantExpressionExpected(expr.SourceTokens)));

    public static Option<ConstantValue, Option<Message>> EvaluateValue(this Node.Expression expr, ReadOnlyScope scope) => expr switch {
        Node.Expression.Literal l => l.Value.Some<ConstantValue, Option<Message>>(),
        Node.Expression.OperationBinary opBin
         => opBin.Operand1.EvaluateValue(scope)
            .Combine(opBin.Operand2.EvaluateValue(scope))
            .FlatMap((op1, op2) => op1.Operate(opBin.Operator, op2)
                .MapError(e => GetOperationMessage(e, opBin, op1.Type, op2.Type).Some())),
        Node.Expression.OperationUnary opUn
         => opUn.Operand.EvaluateValue(scope)
            .FlatMap(op => op.Operate(opUn.Operator)
                .MapError(e => GetOperationMessage(e, opUn, op.Type).Some())),
        Node.Expression.Bracketed b => b.Expression.EvaluateValue(scope),
        Node.Expression.Lvalue.Bracketed b => b.Lvalue.EvaluateValue(scope),
        Node.Expression.Lvalue.VariableReference v
         => scope.GetSymbol<Symbol.Constant>(v.Name)
            .MapError(e => Option.None<Message>())
            .FlatMap(constant => constant.Value.EvaluateValue(scope)),
        _ => Option.None<Message>().None<ConstantValue, Option<Message>>(),
    };

    private static Message GetOperationMessage(OperationError error, Node.Expression.OperationUnary opUn, EvaluatedType operandType) => error switch {
        OperationError.UnsupportedOperator => Message.ErrorUnsupportedOperation(opUn, operandType),
        OperationError.DivisionByZero => Message.WarningDivisionByZero(opUn.SourceTokens),
        _ => throw error.ToUnmatchedException(),
    };

    private static Message GetOperationMessage(OperationError error, Node.Expression.OperationBinary opUn, EvaluatedType op1type, EvaluatedType op2type) => error switch {
        OperationError.UnsupportedOperator => Message.ErrorUnsupportedOperation(opUn, op1type, op2type),
        OperationError.DivisionByZero => Message.WarningDivisionByZero(opUn.SourceTokens),
        _ => throw error.ToUnmatchedException(),
    };
}
