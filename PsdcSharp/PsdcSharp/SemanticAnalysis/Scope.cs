using System.Diagnostics.CodeAnalysis;
using Scover.Psdc.Parsing.Nodes;

namespace Scover.Psdc.StaticAnalysis;

internal sealed class Scope(Scope? parentScope) : ReadOnlyScope
{
    private readonly Scope? _parentScope = parentScope;
    private readonly Dictionary<Node.Identifier, Symbol> _symbolTable = [];

    public IReadOnlyDictionary<Node.Identifier, Symbol> Symbols => _symbolTable;

    public void AddSymbolOrError(Messenger messenger, Symbol symbol)
    {
        if (!TryAdd(symbol, out var existingSymbol)) {
            messenger.Report(Message.ErrorRedefinedSymbol(symbol, existingSymbol));
        }
    }

    public Option<T> GetSymbol<T>(Node.Identifier name) where T : Symbol
     => TryGetSymbol(name, out T? symbol)
        ? symbol.Some()
        : Option.None<T>();
    public Option<T, Message> GetSymbolOrError<T>(Node.Identifier identifier) where T : Symbol
    => GetSymbol<T>(identifier).OrWithError(Message.ErrorUndefinedSymbol<T>(identifier));

    public bool TryGetSymbolOrError<T>(Messenger messenger, Node.Identifier identifier, out T? symbol) where T : Symbol
    {
        bool found = TryGetSymbol<T>(identifier, out symbol);
        if (!found) {
            messenger.Report(Message.ErrorUndefinedSymbol<T>(identifier));
        }
        return found;
    }
    public bool TryAdd(Symbol symbol) => _symbolTable.TryAdd(symbol.Name, symbol);
    public bool TryAdd(Symbol symbol, [NotNullWhen(false)] out Symbol? existingSymbol)
    {
        var added = _symbolTable.TryAdd(symbol.Name, symbol);
        existingSymbol = added ? null : _symbolTable[symbol.Name];
        return added;
    }

    public bool TryGetSymbol<T>(Node.Identifier name, [NotNullWhen(true)] out T? symbol) where T : Symbol
    {
        for (Scope? scope = this; scope is not null; scope = scope._parentScope) {
            if (!scope._symbolTable.TryGetValue(name, out var foundSymbol)) {
                continue;
            }
            if (foundSymbol is not T t) {
                break;
            }

            symbol = t;
            return true;
        }

        symbol = default;
        return false;
    }
}
