
using Scover.Psdc.Parsing;

namespace Scover.Psdc.StaticAnalysis;

internal static partial class AstExtensions
{
    public static Option<EvaluatedType, Message> EvaluateType(this Node.Expression expression, ReadOnlyScope scope) => expression switch {
        Node.Expression.OperationBinary ob => EvaluateTypeOperationBinary(ob, scope),
        Node.Expression.OperationUnary ou => ou.Operand.EvaluateType(scope),
        Node.Expression.Bracketed b => b.Expression.EvaluateType(scope),
        Node.Expression.Lvalue.Bracketed b => b.Lvalue.EvaluateType(scope),
        Node.Expression.Lvalue.ArraySubscript arrSub => arrSub.Array.EvaluateType(scope).FlatMap(outerType
         => outerType is EvaluatedType.Array arrayType
            ? arrayType.ElementType.Some<EvaluatedType, Message>()
            : Message.ErrorSubscriptOfNonArray(arrSub)
                .None<EvaluatedType, Message>()),

        Node.Expression.Lvalue.ComponentAccess compAccess
         => compAccess.Structure.EvaluateType(scope).FlatMap(outerType
            => outerType is not EvaluatedType.Structure structType
                ? Message.ErrrorComponentAccessOfNonStruct(compAccess).None<EvaluatedType, Message>()
                : structType.Components.TryGetValue(compAccess.ComponentName, out var compType)
                ? compType.Some<EvaluatedType, Message>()
                : Message.ErrorStructureComponentDoesntExist(compAccess,
                    scope.Symbols.Values.OfType<Symbol.TypeAlias>()
                    .FirstOrNone(alias => alias.TargetType.SemanticsEqual(structType))
                    .Map(alias => alias.Name))
                .None<EvaluatedType, Message>()),

        Node.Expression.Lvalue.VariableReference varRef
         => scope.GetSymbol<Symbol.Variable>(varRef.Name).Map(var => var.Type),

        Node.Expression.FunctionCall call
         => scope.GetSymbol<Symbol.Function>(call.Name).Map(func => func.ReturnType),
        Node.Expression.Literal lit => lit.Value.Type.Some<EvaluatedType, Message>(),
        _ => throw expression.ToUnmatchedException(),
    };

    public static EvaluatedType CreateTypeOrError(this Node.Type type, ReadOnlyScope scope, Messenger messenger) => type switch {
        Node.Type.String => EvaluatedType.String.Instance,
        NodeAliasReference alias
         => scope.GetSymbol<Symbol.TypeAlias>(alias.Name).MatchError(messenger.Report)
                .Map(aliasType => aliasType.TargetType.ToAliasReference(alias.Name))
            .ValueOr(new EvaluatedType.Unknown(type.SourceTokens)),
        Node.Type.Complete.Array array 
            => EvaluatedType.Array.Create(array.Type.CreateTypeOrError(scope, messenger), array.Dimensions, scope)
            .MatchError(Function.Foreach<Message>(messenger.Report))
            .ValueOr<EvaluatedType>(new EvaluatedType.Unknown(type.SourceTokens)),
        Node.Type.Complete.File file => EvaluatedType.File.Instance,
        Node.Type.Complete.Boolean => EvaluatedType.Boolean.Instance,
        Node.Type.Complete.Character => EvaluatedType.Character.Instance,
        Node.Type.Complete.StringLengthed str
         => EvaluatedType.StringLengthed.Create(str.Length, scope)
            .MatchError(messenger.Report)
            .ValueOr<EvaluatedType>(new EvaluatedType.Unknown(type.SourceTokens)),
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
        return new EvaluatedType.Structure(components);
    }

    private static Option<EvaluatedType, Message> EvaluateTypeOperationBinary(Node.Expression.OperationBinary operationBinary, ReadOnlyScope scope)
     => EvaluateType(operationBinary.Operand1, scope)
        .Combine(EvaluateType(operationBinary.Operand2, scope))
        .FlatMap((o1, o2) => o1.SemanticsEqual(o2)
            ? o1.Some<EvaluatedType, Message>()
            : o1 is EvaluatedType.Numeric n1 && o2 is EvaluatedType.Numeric n2
            ? EvaluatedType.Numeric.GetMostPreciseType(n1, n2)
                .Some<EvaluatedType, Message>()
            : Message.ErrorUnsupportedOperation(operationBinary, o1, o2)
                .None<EvaluatedType, Message>());
}
