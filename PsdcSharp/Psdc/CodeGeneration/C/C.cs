using Scover.Psdc.Pseudocode;
using Scover.Psdc.Parsing;

using static Scover.Psdc.StaticAnalysis.SemanticNode;

namespace Scover.Psdc.CodeGeneration.C;

static class C
{
    public static bool RequiresPointer(ParameterMode mode, EvaluatedType type)
        => mode != ParameterMode.In
        && type is not ArrayType;

    public static bool IsPointerParameter(Expr expr)
        => expr is Expr.Lvalue.VariableReference varRef
        && expr.Meta.Scope.TryGetSymbol<Symbol.Parameter>(varRef.Name, out var param)
        && RequiresPointer(param.Mode, param.Type);
}
