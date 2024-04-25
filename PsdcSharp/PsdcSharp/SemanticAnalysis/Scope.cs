using System.Diagnostics.CodeAnalysis;

namespace Scover.Psdc.SemanticAnalysis;

internal interface ReadOnlyScope
{
    Option<T> GetSymbol<T>(string name) where T : Symbol;
    bool TryGetSymbol<T>(string name, [NotNullWhen(true)] out T? symbol) where T : Symbol;
}

internal sealed class Scope(Scope? parentScope) : ReadOnlyScope
{
    private readonly Scope? _parentScope = parentScope;
    private readonly Dictionary<string, Symbol> _symbolTable = [];

    public IReadOnlyDictionary<string, Symbol> Symbols => _symbolTable;

    public Option<T> GetSymbol<T>(string name) where T : Symbol
     => TryGetSymbol(name, out T? symbol)
        ? symbol.Some()
        : Option.None<T>();

    public bool TryAdd(Symbol symbol) => _symbolTable.TryAdd(symbol.Name, symbol);
    public bool TryAdd(Symbol symbol, [NotNullWhen(false)] out Symbol? existingSymbol)
    {
        var added = _symbolTable.TryAdd(symbol.Name, symbol);
        existingSymbol = added ? null : _symbolTable[symbol.Name];
        return added;
    }

    public bool TryGetSymbol<T>(string name, [NotNullWhen(true)] out T? symbol) where T : Symbol
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
