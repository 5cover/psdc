
using System.Globalization;
using Scover.Psdc.Language;
using Scover.Psdc.Messages;
using Scover.Psdc.Parsing;
using static Scover.Psdc.Parsing.Node;
using Type = Scover.Psdc.Parsing.Node.Type;

namespace Scover.Psdc.StaticAnalysis;

internal static partial class AstExtensions
{
    public static Option<EvaluatedType, Message> EvaluateType(this Expression expression, ReadOnlyScope scope) => expression switch {
        Expression.BinaryOperation ob => EvaluateTypeOperationBinary(ob, scope),
        Expression.UnaryOperation ou => ou.Operand.EvaluateType(scope),
        Expression.Bracketed b => b.ContainedExpression.EvaluateType(scope),
        Expression.Lvalue.Bracketed b => b.ContainedLvalue.EvaluateType(scope),

        Expression.Lvalue.ArraySubscript arrSub => arrSub.Array.EvaluateType(scope).FlatMap(outerType
         => outerType is EvaluatedType.Array arrayType
            ? arrayType.ElementType.Some<EvaluatedType, Message>()
            : Message.ErrorSubscriptOfNonArray(arrSub)
                .None<EvaluatedType, Message>()),

        Expression.Lvalue.ComponentAccess compAccess
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

        Expression.Lvalue.VariableReference varRef
         => scope.GetSymbol<Symbol.Variable>(varRef.Name).Map(var => var.Type),

        Expression.FunctionCall call
         => scope.GetSymbol<Symbol.Function>(call.Name).Map(func => func.ReturnType),
        Expression.Literal lit => lit.Value.Type.Some<EvaluatedType, Message>(),

        _ => throw expression.ToUnmatchedException(),
    };

    public static EvaluatedType CreateTypeOrError(this Type type, ReadOnlyScope scope, Messenger messenger) => type switch {
        Type.String => EvaluatedType.String.Instance,
        NodeAliasReference alias
         => scope.GetSymbol<Symbol.TypeAlias>(alias.Name).MatchError(messenger.Report)
                .Map(aliasType => aliasType.TargetType.ToAliasReference(alias.Name))
            .ValueOr(new EvaluatedType.Unknown(type.SourceTokens)),
        Type.Complete.Array array
            => EvaluatedType.Array.Create(array.Type.CreateTypeOrError(scope, messenger), array.Dimensions, scope)
            .MatchError(Function.Foreach<Message>(messenger.Report))
            .ValueOr<EvaluatedType>(new EvaluatedType.Unknown(type.SourceTokens)),
        Type.Complete.File file => EvaluatedType.File.Instance,
        Type.Complete.Boolean => EvaluatedType.Boolean.Instance,
        Type.Complete.Character => EvaluatedType.Character.Instance,
        Type.Complete.StringLengthed str
         => EvaluatedType.StringLengthed.Create(str.Length, scope)
            .MatchError(messenger.Report)
            .ValueOr<EvaluatedType>(new EvaluatedType.Unknown(type.SourceTokens)),
        Type.Complete.Structure structure => CreateStructureTypeOrError(structure, scope, messenger),
        Type.Complete.Numeric p => EvaluatedType.Numeric.GetInstance(p.Type),
        _ => throw type.ToUnmatchedException(),
    };

    public static EvaluatedType.Structure CreateStructureTypeOrError(Type.Complete.Structure structure, ReadOnlyScope scope, Messenger messenger)
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

    private static Option<EvaluatedType, Message> EvaluateTypeOperationBinary(Expression.BinaryOperation operationBinary, ReadOnlyScope scope)
     => EvaluateType(operationBinary.Left, scope)
        .Combine(EvaluateType(operationBinary.Right, scope))
        .FlatMap((o1, o2) => o1.SemanticsEqual(o2)
            ? o1.Some<EvaluatedType, Message>()
            : o1 is EvaluatedType.Numeric n1 && o2 is EvaluatedType.Numeric n2
            ? EvaluatedType.Numeric.GetMostPreciseType(n1, n2)
                .Some<EvaluatedType, Message>()
            : Message.ErrorUnsupportedOperation(operationBinary, o1, o2)
                .None<EvaluatedType, Message>());

    public static Expression.BinaryOperation Alter(this Expression expr, BinaryOperator @operator, int value)
    // This feels like cheating, creatig AST nodes with placeholder source tokens during code generation
    // But this is the only way without massive abstraction
    // This will be improved if it ever becomes a problem.
     => new(SourceTokens.Empty, expr, @operator,
            new Expression.Literal.Integer(SourceTokens.Empty, value.ToString(CultureInfo.InvariantCulture)));
}
