using System.Diagnostics.CodeAnalysis;

using Scover.Psdc.Messages;
using Scover.Psdc.Parsing;

namespace Scover.Psdc.Pseudocode;

public abstract class Scope(Scope? parent)
{
    private protected readonly Dictionary<string, Symbol> SymbolTable = [];
    private protected readonly Scope? ParentScope = parent;

    public IEnumerable<T> GetSymbols<T>() where T : Symbol
    {
        for (var scope = this; scope is not null; scope = scope.ParentScope) {
            foreach (T t in scope.SymbolTable.Values.OfType<T>()) {
                yield return t;
            }
        }
    }

    public Option<T, Message> GetSymbol<T>(Ident name) where T : Symbol
     => !TryGetSymbol(name.ToString(), out var symbol)
        ? Message.ErrorUndefinedSymbol<T>(name).None<T, Message>()
        : symbol is not T t
        ? Message.ErrorUndefinedSymbol<T>(name, symbol).None<T, Message>()
        : t.Some<T, Message>();

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
        for (var scope = this; scope is not null; scope = scope.ParentScope) {
            if (scope.SymbolTable.TryGetValue(name, out symbol)) {
                return true;
            }
        }

        symbol = default;
        return false;
    }

    public bool TryGetSymbol<T>(Ident name, [NotNullWhen(true)] out T? symbol) where T : Symbol => TryGetSymbol(name.ToString(), out symbol);
    public bool TryGetSymbol(Ident name, [NotNullWhen(true)] out Symbol? symbol) => TryGetSymbol(name.ToString(), out symbol);

    public bool HasSymbol(Ident name) => HasSymbol(name.ToString());
    public bool HasSymbol(string name) => SymbolTable.ContainsKey(name);
}
