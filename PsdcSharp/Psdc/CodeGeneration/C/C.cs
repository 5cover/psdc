using Scover.Psdc.Language;
using Scover.Psdc.Parsing;
using static Scover.Psdc.Parsing.Node;

namespace Scover.Psdc.CodeGeneration.C;

static class C
{
    public static bool RequiresPointer(this ParameterMode mode) => mode != ParameterMode.In;

    public static bool IsPointer(this ReadOnlyScope scope, Expression expr)
     => expr is Expression.Lvalue.VariableReference varRef
     && scope.TryGetSymbol<Symbol.Parameter>(varRef.Name, out var param)
     && param.Mode != ParameterMode.In;
}
