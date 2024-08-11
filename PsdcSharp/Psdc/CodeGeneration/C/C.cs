using Scover.Psdc.Language;
using Scover.Psdc.Parsing;
using static Scover.Psdc.StaticAnalysis.SemanticNode;

namespace Scover.Psdc.CodeGeneration.C;

static class C
{
    public static bool RequiresPointer(ParameterMode mode) => mode != ParameterMode.In;

    public static bool IsPointer(Expression expr)
     => expr is Expression.Lvalue.VariableReference varRef
     && expr.Meta.Scope.TryGetSymbol<Symbol.Parameter>(varRef.Name, out var param)
     && param.Mode != ParameterMode.In;
}
