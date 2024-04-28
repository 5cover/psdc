
using Scover.Psdc.Parsing;

namespace Scover.Psdc.StaticAnalysis;

internal static class AstExtensions
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

    public static Option<Option<string>, Message> EvaluateValue(this Node.Expression expr, ReadOnlyScope scope) => expr switch {
        Node.Expression.Literal l => l.Value.Some().Some<Option<string>, Message>(),
        Node.Expression.Lvalue.VariableReference v => scope.GetSymbol<Symbol.Constant>(v.Name).FlatMap(constant => constant.Value.EvaluateValue(scope)),
        _ => Option.None<string>().Some<Option<string>, Message>(),
    };

    public static Option<EvaluatedType, Message> EvaluateType(this Node.Expression expression, ReadOnlyScope scope) => expression switch {
        Node.Expression.OperationBinary ob => EvaluateTypeOperationBinary(ob, scope),
        Node.Expression.OperationUnary ou => ou.Operand.EvaluateType(scope),
        Node.Expression.Bracketed b => b.Expression.EvaluateType(scope),
        Node.Expression.Lvalue.Bracketed b => b.Lvalue.EvaluateType(scope),
        Node.Expression.Lvalue.ArraySubscript arrSub => arrSub.Array.EvaluateType(scope).FlatMap(outerType
         => outerType.Unwrap() is EvaluatedType.Array arrayType
            ? arrayType.ElementType.Some<EvaluatedType, Message>()
            : Option.None<EvaluatedType, Message>(Message.ErrorSubscriptOfNonArray(arrSub))),
        Node.Expression.Lvalue.ComponentAccess compAccess
         => compAccess.Structure.EvaluateType(scope).FlatMap(outerType
            => outerType.Unwrap() is not EvaluatedType.Structure structType
                ? Option.None<EvaluatedType, Message>(Message.ErrrorComponentAccessOfNonStruct(compAccess))

                : structType.Components.TryGetValue(compAccess.ComponentName, out var compType)
                ? compType.Some<EvaluatedType, Message>()

                : Option.None<EvaluatedType, Message>(Message.ErrorStructureComponentDoesntExist(compAccess,
                    scope.Symbols.Values.OfType<Symbol.TypeAlias>()
                    .FirstOrNone(alias => alias.TargetType.Equals(structType))
                        .Map(alias => alias.Name)))),
        Node.Expression.Lvalue.VariableReference varRef
         => scope.GetSymbol<Symbol.Variable>(varRef.Name).Map(var => var.Type),
        Node.Expression.True or Node.Expression.False
         => EvaluatedType.Numeric.GetInstance(NumericType.Boolean).Some<EvaluatedType, Message>(),
        Node.Expression.Literal.Character
         => EvaluatedType.Numeric.GetInstance(NumericType.Character).Some<EvaluatedType, Message>(),
        Node.Expression.Literal.Integer
         => EvaluatedType.Numeric.GetInstance(NumericType.Integer).Some<EvaluatedType, Message>(),
        Node.Expression.Literal.Real
         => EvaluatedType.Numeric.GetInstance(NumericType.Real).Some<EvaluatedType, Message>(),
        Node.Expression.Literal.String str
         => new EvaluatedType.StringLengthedKnown(str.Value.Length).Some<EvaluatedType, Message>(),
        Node.Expression.FunctionCall call
         => scope.GetSymbol<Symbol.Function>(call.Name).Map(func => func.ReturnType),
        _ => throw expression.ToUnmatchedException(),
    };

    public static EvaluatedType CreateTypeOrError(this Node.Type type, ReadOnlyScope scope, Messenger messenger) => type switch {
        Node.Type.String => EvaluatedType.String.Instance,
        NodeAliasReference alias
         => scope.GetSymbol<Symbol.TypeAlias>(alias.Name).MapError(messenger.Report)
                .Map(aliasType => (EvaluatedType)new EvaluatedType.AliasReference(alias.Name, aliasType.TargetType))
            .ValueOr(new EvaluatedType.Unknown(type.SourceTokens)),
        Node.Type.Complete.Array array => new EvaluatedType.Array(array.Type.CreateTypeOrError(scope, messenger), array.Dimensions),
        Node.Type.Complete.File file => EvaluatedType.File.Instance,
        Node.Type.Complete.LengthedString str => new EvaluatedType.LengthedString(str.Length),
        Node.Type.Complete.Structure structure => CreateStructureTypeOrError(structure, scope, messenger),
        Node.Type.Complete.Numeric p => EvaluatedType.Numeric.GetInstance(p.Type),
        _ => throw type.ToUnmatchedException(),
    };

    public static EvaluatedType.Structure CreateStructureTypeOrError(Node.Type.Complete.Structure structure, ReadOnlyScope scope, Messenger messenger)
    {
        Dictionary<Identifier, EvaluatedType> components = [];
        foreach (var comp in structure.Components) {
            foreach (var name in comp.Names) {
                if (!components.TryAdd(name, comp.Type.CreateTypeOrError(scope, messenger))) {
                    messenger.Report(Message.ErrorStructureDuplicateComponent(comp.SourceTokens, name));
                }
            }
        }

        // Don't create the structure if we weren't able to create all components names. This will prevent further errors when using the structure.
        return new EvaluatedType.Structure(components);
    }

    private static Option<EvaluatedType, Message> EvaluateTypeOperationBinary(Node.Expression.OperationBinary operationBinary, ReadOnlyScope scope)
     => EvaluateType(operationBinary.Operand1, scope).Combine(EvaluateType(operationBinary.Operand2, scope)).FlatMap((o1, o2) => o1.Equals(o2)
        ? o1.Some<EvaluatedType, Message>()
        : o1 is EvaluatedType.Numeric n1 && o2 is EvaluatedType.Numeric n2
        ? EvaluatedType.Numeric.GetMostPreciseType(n1, n2).Some<EvaluatedType, Message>()
        : Option.None<EvaluatedType, Message>(
            Message.UnsupportedOperandTypesForBinaryOperation(operationBinary.SourceTokens, o1, o2)));
}
