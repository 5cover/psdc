using System.Diagnostics.CodeAnalysis;
using Scover.Psdc.Parsing.Nodes;

namespace Scover.Psdc.SemanticAnalysis;

internal interface ReadOnlyScope
{
    public IReadOnlyDictionary<Node.Identifier, Symbol> Symbols { get; }
    public Option<T> GetSymbol<T>(Node.Identifier name) where T : Symbol;
    public bool TryGetSymbol<T>(Node.Identifier name, [NotNullWhen(true)] out T? symbol) where T : Symbol;

    public Option<T, Message> GetSymbolOrError<T>(Node.Identifier identifier) where T : Symbol;

    public bool TryGetSymbolOrError<T>(MessageProvider msgProvider, Node.Identifier identifier, out T? symbol) where T : Symbol;
}
