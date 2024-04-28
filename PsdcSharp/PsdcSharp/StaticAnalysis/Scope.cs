using System.Diagnostics.CodeAnalysis;
using Scover.Psdc.Parsing;

namespace Scover.Psdc.StaticAnalysis;

internal sealed class Scope(Scope? scope) : ReadOnlyScope
{
    private readonly Scope? _parentScope = scope;
    private readonly Dictionary<Identifier, Symbol> _symbolTable = [];

    public IReadOnlyDictionary<Identifier, Symbol> Symbols => _symbolTable;

    public void AddSymbolOrError(Messenger messenger, Symbol symbol)
    {
        if (!TryAdd(symbol, out var existingSymbol)) {
            messenger.Report(Message.ErrorRedefinedSymbol(symbol, existingSymbol));
        }
    }

    public Option<T, Message> GetSymbol<T>(Identifier name) where T : Symbol
     => !TryGetSymbol(name, out var symbol)
        ? Option.None<T, Message>(Message.ErrorUndefinedSymbol<T>(name))
        : symbol is not T t
        ? Option.None<T, Message>(Message.ErrorUndefinedSymbol<T>(name, symbol))
        : t.Some<T, Message>();

    public bool TryGetSymbol<T>(Identifier name, [NotNullWhen(true)] out T? symbol) where T : Symbol
    {
        if (TryGetSymbol(name, out var foundSymbol) && foundSymbol is T t) {
            symbol = t;
            return true;
        }
        symbol = default;
        return false;
    }

    public bool TryAdd(Symbol symbol) => _symbolTable.TryAdd(symbol.Name, symbol);

    public bool TryAdd(Symbol symbol, [NotNullWhen(false)] out Symbol? existingSymbol)
    {
        var added = _symbolTable.TryAdd(symbol.Name, symbol);
        existingSymbol = added ? null : _symbolTable[symbol.Name];
        return added;
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
