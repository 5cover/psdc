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
     => TryGetSymbol(name, out T? symbol)
        ? symbol.Some<T, Message>()
        : Option.None<T, Message>(Message.ErrorUndefinedSymbol<T>(name));

    public bool TryGetSymbolOrError<T>(Messenger messenger, Identifier name, out T? symbol) where T : Symbol
    {
        bool found = TryGetSymbol(name, out symbol);
        if (!found) {
            messenger.Report(Message.ErrorUndefinedSymbol<T>(name));
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

    public bool TryGetSymbol<T>(Identifier name, [NotNullWhen(true)] out T? symbol) where T : Symbol
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
