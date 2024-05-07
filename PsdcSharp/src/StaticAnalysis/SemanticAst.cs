using Scover.Psdc.Language;
using Scover.Psdc.Parsing;

using static Scover.Psdc.Parsing.Node;

namespace Scover.Psdc.StaticAnalysis;

sealed class SemanticAst(
    Algorithm root,
    IReadOnlyDictionary<NodeScoped, Scope> scopes,
    IReadOnlyDictionary<Expression, Option<EvaluatedType>> inferredTypes)
{
    public IReadOnlyDictionary<Expression, Option<EvaluatedType>> InferredTypes => inferredTypes;
    public Algorithm Root => root;
    public IReadOnlyDictionary<NodeScoped, Scope> Scopes => scopes;
}
