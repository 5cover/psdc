using Scover.Psdc.Language;
using Scover.Psdc.Parsing;

using static Scover.Psdc.Parsing.Node;

namespace Scover.Psdc.StaticAnalysis;

public sealed class SemanticAst
{
    internal SemanticAst(
        Algorithm root,
        IReadOnlyDictionary<NodeScoped, Scope> scopes,
        IReadOnlyDictionary<Expression, Option<EvaluatedType>> inferredTypes) {
        Root = root;
        Scopes = scopes;
        InferredTypes = inferredTypes;
    }

    internal IReadOnlyDictionary<Expression, Option<EvaluatedType>> InferredTypes { get; }
    internal Algorithm Root { get; }
    internal IReadOnlyDictionary<NodeScoped, Scope> Scopes { get; }

}
