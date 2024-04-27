using Scover.Psdc.Parsing.Nodes;
using Scover.Psdc.StaticAnalysis;

namespace Scover.Psdc.CodeGeneration;

internal static class ExpressionExtensions
{
    private static readonly IReadOnlyDictionary<PrimitiveType, int> integralTypePrecisions = new Dictionary<PrimitiveType, int> {
        [PrimitiveType.Boolean] = 0,
        [PrimitiveType.Character] = 1,
        [PrimitiveType.Integer] = 2,
        [PrimitiveType.Real] = 3,
    };

    public static bool IsConstant(this Node.Expression expression, ReadOnlyScope scope) => expression switch {
        Node.Expression.Bracketed b => b.Expression.IsConstant(scope),
        Node.Expression.Literal => true,
        Node.Expression.LValue.ArraySubscript arrSub => arrSub.Array.IsConstant(scope) && arrSub.Indexes.All(i => i.IsConstant(scope)),
        Node.Expression.LValue.Bracketed b => b.LValue.IsConstant(scope),
        Node.Expression.LValue.ComponentAccess compAccess => compAccess.Structure.IsConstant(scope),
        Node.Expression.LValue.VariableReference varRef => scope.GetSymbol<Symbol.Constant>(varRef.Name).HasValue,
        Node.Expression.OperationBinary ob => ob.Operand1.IsConstant(scope) && ob.Operand2.IsConstant(scope),
        Node.Expression.OperationUnary ou => ou.Operand.IsConstant(scope),
        _ => throw expression.ToUnmatchedException(),
    };

    public static Option<EvaluatedType, Message> EvaluateType(this Node.Expression expression, ReadOnlyScope scope) => expression switch {
        Node.Expression.OperationBinary ob => EvaluateTypeOperationBinary(ob, scope),
        Node.Expression.OperationUnary ou => ou.Operand.EvaluateType(scope),
        Node.Expression.Bracketed b => b.Expression.EvaluateType(scope),
        Node.Expression.LValue.Bracketed b => b.LValue.EvaluateType(scope),
        Node.Expression.LValue.ArraySubscript arrSub => arrSub.Array.EvaluateType(scope).FlatMap(arrayExprType
         => arrayExprType is EvaluatedType.Array arrayType
            ? arrayType.ElementType.Some<EvaluatedType, Message>()
            : Option.None<EvaluatedType, Message>(Message.ErrorSubscriptOfNonArray(arrSub))),
        Node.Expression.LValue.ComponentAccess compAccess
         => compAccess.Structure.EvaluateType(scope).FlatMap(outerType
            => outerType is not EvaluatedType.Structure structType
                ? Option.None<EvaluatedType, Message>(Message.ErrrorComponentAccessOfNonStruct(compAccess))

                : structType.Components.TryGetValue(compAccess.ComponentName, out var compType)
                ? compType.Some<EvaluatedType, Message>()

                : Option.None<EvaluatedType, Message>(Message.ErrorStructureComponentDoesntExist(compAccess,
                    scope.Symbols.Values.OfType<Symbol.TypeAlias>()
                    .FirstOrNone(alias => alias.TargetType.Equals(structType))
                        .Map(alias => alias.Name)))),
        Node.Expression.LValue.VariableReference varRef
         => scope.GetSymbol<Symbol.Variable>(varRef.Name)
            .Map(var => var.Type).OrWithError(Message.ErrorUndefinedSymbol<Symbol.Variable>(varRef.Name)),
        Node.Expression.Literal.True or Node.Expression.Literal.False
         => new EvaluatedType.Primitive(PrimitiveType.Boolean).Some<EvaluatedType, Message>(),
        Node.Expression.Literal.Character
         => new EvaluatedType.Primitive(PrimitiveType.Character).Some<EvaluatedType, Message>(),
        Node.Expression.Literal.Integer
         => new EvaluatedType.Primitive(PrimitiveType.Integer).Some<EvaluatedType, Message>(),
        Node.Expression.Literal.Real
         => new EvaluatedType.Primitive(PrimitiveType.Real).Some<EvaluatedType, Message>(),
        Node.Expression.Literal.String str
         => new EvaluatedType.StringLengthedKnown(str.Value.Length).Some<EvaluatedType, Message>(),
        Node.Expression.FunctionCall call
         => scope.GetSymbolOrError<Symbol.Function>(call.Name).Map(func => func.ReturnType),
        _ => throw expression.ToUnmatchedException(),
    };

    private static Option<EvaluatedType, Message> EvaluateTypeOperationBinary(Node.Expression.OperationBinary operationBinary, ReadOnlyScope scope)
     => EvaluateType(operationBinary.Operand1, scope).Combine(EvaluateType(operationBinary.Operand2, scope)).FlatMap((o1, o2) => o1.Equals(o2)
        ? o1.Some<EvaluatedType, Message>()
        : FindCommonType(o1, o2).OrWithError(
            Message.UnsupportedOperandTypesForBinaryOperation(operationBinary.SourceTokens, o1, o2)));

    private static Option<EvaluatedType> FindCommonType(EvaluatedType type1, EvaluatedType type2)
     => type1 is EvaluatedType.Primitive { IsNumeric: true } p1
     && type2 is EvaluatedType.Primitive { IsNumeric: true } p2
        ? (integralTypePrecisions[p1.Type] > integralTypePrecisions[p2.Type]
            ? type1
            : type2)
            .Some<EvaluatedType>()
        : Option.None<EvaluatedType>();

    public static Option<string> EvaluateValue(this Node.Expression expr, ReadOnlyScope scope) => expr switch {
        Node.Expression.Literal l => l.Value.Some(),
        Node.Expression.LValue.VariableReference v => scope.GetSymbol<Symbol.Constant>(v.Name).FlatMap(constant => constant.Value.EvaluateValue(scope)),
        _ => Option.None<string>(),
    };
}
