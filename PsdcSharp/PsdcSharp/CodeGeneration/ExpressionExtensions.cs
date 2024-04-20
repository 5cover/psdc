using Scover.Psdc.Parsing.Nodes;
using Scover.Psdc.SemanticAnalysis;

namespace Scover.Psdc.CodeGeneration;

internal static class ExpressionExtensions
{
    private static readonly IReadOnlyDictionary<PrimitiveType, int> integralTypePrecisions = new Dictionary<PrimitiveType, int> {
        [PrimitiveType.Boolean] = 0,
        [PrimitiveType.Character] = 1,
        [PrimitiveType.Integer] = 2,
        [PrimitiveType.Real] = 3,
    };

    public static bool IsConstant(this Node.Expression expression) => expression switch {
        Node.Expression.OperationBinary ob => IsConstant(ob.Operand1) && IsConstant(ob.Operand2),
        Node.Expression.OperationUnary ou => IsConstant(ou.Operand),
        Node.Expression.Literal => true,
        Node.Expression.Bracketed b => IsConstant(b.Expression),
        _ => throw expression.ToUnmatchedException(),
    };

    public static Option<EvaluatedType, ErrorProvider> EvaluateType(this Node.Expression expression, ReadOnlyScope scope) => expression switch {
        Node.Expression.OperationBinary ob => EvaluateTypeOperationBinary(ob, scope),
        Node.Expression.OperationUnary ou => EvaluateType(ou.Operand, scope),
        Node.Expression.Bracketed b => EvaluateType(b.Expression, scope),
        Node.Expression.VariableReference variable
         => scope.GetSymbol<Symbol.Variable>(variable.Name)
            .Map(var => var.Type).OrWithError((ErrorProvider)(tokens => Message.ErrorUndefinedSymbol<Symbol.Variable>(tokens, variable.Name))),
        Node.Expression.Literal.True or Node.Expression.Literal.False
         => new EvaluatedType.Primitive(PrimitiveType.Boolean).Some<EvaluatedType, ErrorProvider>(),
        Node.Expression.Literal.Character
         => new EvaluatedType.Primitive(PrimitiveType.Character).Some<EvaluatedType, ErrorProvider>(),
        Node.Expression.Literal.Integer
         => new EvaluatedType.Primitive(PrimitiveType.Integer).Some<EvaluatedType, ErrorProvider>(),
        Node.Expression.Literal.Real
         => new EvaluatedType.Primitive(PrimitiveType.Real).Some<EvaluatedType, ErrorProvider>(),
        Node.Expression.Literal.String str
         => new EvaluatedType.StringLengthedKnown(str.Value.Length).Some<EvaluatedType, ErrorProvider>(),
        _ => throw expression.ToUnmatchedException(),
    };

    private static Option<EvaluatedType, ErrorProvider> EvaluateTypeOperationBinary(Node.Expression.OperationBinary operationBinary, ReadOnlyScope scope)
    {
        var operand1 = EvaluateType(operationBinary.Operand1, scope);

        return operand1.FlatMap(
            o1 => EvaluateType(operationBinary.Operand2, scope).FlatMap(
                o2 => o1.Equals(o2)
                    ? operand1
                    : FindCommonType(o1, o2)));
    }

    private static Option<EvaluatedType, ErrorProvider> FindCommonType(EvaluatedType type1, EvaluatedType type2)
     => type1 is EvaluatedType.Primitive { IsNumeric: true } p1
     && type2 is EvaluatedType.Primitive { IsNumeric: true } p2
        ? (integralTypePrecisions[p1.Type] > integralTypePrecisions[p2.Type]
            ? type1
            : type2)
            .Some<EvaluatedType, ErrorProvider>()
        : Option.None<EvaluatedType, ErrorProvider>(Message.ErrorCantInferTypeOfExpression);

    public static Option<string> EvaluateValue(this Node.Expression expr, ReadOnlyScope scope) => expr switch {
        Node.Expression.Literal l => l.Value.Some(),
        Node.Expression.VariableReference v => scope.GetSymbol<Symbol.Constant>(v.Name).FlatMap(constant => constant.Value.EvaluateValue(scope)),
        _ => Option.None<string>(),
    };
}
