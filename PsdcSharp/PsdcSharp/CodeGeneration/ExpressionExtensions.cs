using Scover.Psdc.Parsing;
using Scover.Psdc.Parsing.Nodes;

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
        Node.Expression.OperationBinary ob => ob.Operand1.Map(IsConstant).ValueOr(false) && ob.Operand2.Map(IsConstant).ValueOr(false),
        Node.Expression.OperationUnary ou => ou.Operand.Map(IsConstant).ValueOr(false),
        Node.Expression.Literal => true,
        Node.Expression.Bracketed b => b.Expression.Map(IsConstant).ValueOr(false),
        _ => throw expression.ToUnmatchedException(),
    };

    public static Option<TypeInfo, ErrorProvider> EvaluateType(this Node.Expression expression, ReadOnlyScope scope) => expression switch {
        Node.Expression.OperationBinary ob => EvaluateTypeOperationBinary(ob, scope),
        Node.Expression.OperationUnary ou => EvaluateOperandType(ou.Operand, scope),
        Node.Expression.Literal.True or Node.Expression.Literal.False
         => TypeInfo.Primitive.Create(PrimitiveType.Boolean).Some<TypeInfo, ErrorProvider>(),
        Node.Expression.Literal.Character => TypeInfo.Primitive.Create(PrimitiveType.Character).Some<TypeInfo, ErrorProvider>(),
        Node.Expression.Literal.Integer => TypeInfo.Primitive.Create(PrimitiveType.Integer).Some<TypeInfo, ErrorProvider>(),
        Node.Expression.Literal.Real => TypeInfo.Primitive.Create(PrimitiveType.Real).Some<TypeInfo, ErrorProvider>(),
        Node.Expression.Literal.String str => TypeInfo.CreateLengthedString(str.Value.Length.ToString()).Some<TypeInfo, ErrorProvider>(),
        Node.Expression.Bracketed b => b.Expression.Match(
            some: expr => EvaluateType(expr, scope),
            none: error => Option.None<TypeInfo, ErrorProvider>(tokens
             => Message.SyntaxError<Node.Expression.Bracketed>(b.Expression.SourceTokens, error))),
        Node.Expression.Variable variable
         => scope.GetSymbol<Symbol.Variable>(variable.Name)
            .Map(var => var.Type).OrWithError((ErrorProvider)(tokens => Message.UndefinedSymbol<Symbol.Variable>(tokens, variable.Name))),
        _ => throw expression.ToUnmatchedException(),
    };
    private static Option<TypeInfo, ErrorProvider> EvaluateOperandType(ParseResult<Node.Expression> operand, ReadOnlyScope scope) => operand.Match(
    op => EvaluateType(op, scope),
    error => Option.None<TypeInfo, ErrorProvider>(_
     => Message.SyntaxError<Node.Expression>(operand.SourceTokens, error)));

    private static Option<TypeInfo, ErrorProvider> EvaluateTypeOperationBinary(Node.Expression.OperationBinary operationBinary, ReadOnlyScope scope)
    {
        Option<TypeInfo, ErrorProvider> operand1 = EvaluateOperandType(operationBinary.Operand1, scope);

        return operand1.FlatMap(
            o1 => EvaluateOperandType(operationBinary.Operand2, scope).FlatMap(
                o2 => o1.Equals(o2)
                    ? operand1
                    : FindCommonType(o1, o2)));
    }

    private static Option<TypeInfo, ErrorProvider> FindCommonType(TypeInfo type1, TypeInfo type2)
     => type1 is TypeInfo.Primitive { IsNumeric: true } p1 && type2 is TypeInfo.Primitive { IsNumeric: true } p2
        ? (integralTypePrecisions[p1.Type] > integralTypePrecisions[p2.Type]
            ? type1
            : type2)
            .Some<TypeInfo, ErrorProvider>()
        : Option.None<TypeInfo, ErrorProvider>(Message.CantInferType);
}
