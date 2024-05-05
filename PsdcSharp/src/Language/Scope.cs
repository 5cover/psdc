using System.Diagnostics.CodeAnalysis;

using Scover.Psdc.Messages;
using Scover.Psdc.Parsing;

namespace Scover.Psdc.Language;

sealed class Scope(Scope? scope) : ReadOnlyScope
{
    readonly Scope? _parentScope = scope;
    readonly Dictionary<Identifier, Symbol> _symbolTable = [];

    public IReadOnlyDictionary<Identifier, Symbol> Symbols => _symbolTable;

    public void AddSymbolOrError(Messenger messenger, Symbol symbol)
    {
        if (!TryAdd(symbol, out var existingSymbol)) {
            messenger.Report(Message.ErrorRedefinedSymbol(symbol, existingSymbol));
        }
    }

    public Option<T, Message> GetSymbol<T>(Identifier name) where T : Symbol
     => !TryGetSymbol(name, out var symbol)
        ? Message.ErrorUndefinedSymbol<T>(name).None<T, Message>()
        : symbol is not T t
        ? Message.ErrorUndefinedSymbol<T>(name, symbol).None<T, Message>()
        : t.Some<T, Message>();

    public bool TryAdd(Symbol symbol) => _symbolTable.TryAdd(symbol.Name, symbol);

    public bool TryAdd(Symbol symbol, [NotNullWhen(false)] out Symbol? existingSymbol)
    {
        var added = _symbolTable.TryAdd(symbol.Name, symbol);
        existingSymbol = added ? null : _symbolTable[symbol.Name];
        return added;
    }

    public bool TryGetSymbol<T>(Identifier name, [NotNullWhen(true)] out T? symbol) where T : Symbol
    {
        if (TryGetSymbol(name, out var foundSymbol) && foundSymbol is T t) {
            symbol = t;
            return true;
        }
        symbol = default;
        return false;
    }

    public bool TryGetSymbol(Identifier name, [NotNullWhen(true)] out Symbol? symbol)
    {
        for (Scope? scope = this; scope is not null; scope = scope._parentScope) {
            if (scope._symbolTable.TryGetValue(name, out symbol)) {
                return true;
            }
        }

        symbol = default;
        return false;
    }
}
