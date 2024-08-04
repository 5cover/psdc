using System.Diagnostics.CodeAnalysis;

using Scover.Psdc.Messages;
using Scover.Psdc.Parsing;

namespace Scover.Psdc.Language;

sealed class Scope(Scope? scope) : ReadOnlyScope
{
    readonly Scope? _parentScope = scope;
    readonly Dictionary<string, Symbol> _symbolTable = [];

    public void AddSymbolOrError(Messenger messenger, Symbol symbol)
    {
        if (!TryAdd(symbol, out var existingSymbol)) {
            messenger.Report(Message.ErrorRedefinedSymbol(symbol, existingSymbol));
        }
    }

    public Option<T, Message> GetSymbol<T>(Identifier name) where T : Symbol
     => !TryGetSymbol(name.Name, out var symbol)
        ? Message.ErrorUndefinedSymbol<T>(name).None<T, Message>()
        : symbol is not T t
        ? Message.ErrorUndefinedSymbol<T>(name, symbol).None<T, Message>()
        : t.Some<T, Message>();
    public bool TryAdd(Symbol symbol) => _symbolTable.TryAdd(symbol.Name.Name, symbol);

    public bool TryAdd(Symbol symbol, [NotNullWhen(false)] out Symbol? existingSymbol)
    {
        var added = _symbolTable.TryAdd(symbol.Name.Name, symbol);
        existingSymbol = added ? null : _symbolTable[symbol.Name.Name];
        return added;
    }

    public bool TryGetSymbol<T>(string name, [NotNullWhen(true)] out T? symbol) where T : Symbol
    {
        if (TryGetSymbol(name, out var foundSymbol) && foundSymbol is T t) {
            symbol = t;
            return true;
        }
        symbol = default;
        return false;
    }

    public bool TryGetSymbol(string name, [NotNullWhen(true)] out Symbol? symbol)
    {
        for (Scope? scope = this; scope is not null; scope = scope._parentScope) {
            if (scope._symbolTable.TryGetValue(name, out symbol)) {
                return true;
            }
        }

        symbol = default;
        return false;
    }

    public bool TryGetSymbol<T>(Identifier name, [NotNullWhen(true)] out T? symbol) where T : Symbol => TryGetSymbol(name.Name, out symbol);
    public bool TryGetSymbol(Identifier name, [NotNullWhen(true)] out Symbol? symbol) => TryGetSymbol(name.Name, out symbol);

    public bool HasSymbol(Identifier name) => HasSymbol(name.Name);
    public bool HasSymbol(string name) => _symbolTable.ContainsKey(name);
}
