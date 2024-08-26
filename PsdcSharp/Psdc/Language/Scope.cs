using System.Diagnostics.CodeAnalysis;

using Scover.Psdc.Messages;
using Scover.Psdc.Parsing;

namespace Scover.Psdc.Language;

public abstract class Scope(Scope? parent)
{
    private protected readonly Dictionary<string, Symbol> _symbolTable = [];
    private protected readonly Scope? _parentScope = parent;

    public IEnumerable<T> GetSymbols<T>() where T : Symbol
    {
        for (var scope = this; scope is not null; scope = scope._parentScope) {
            foreach (T t in scope._symbolTable.Values.OfType<T>()) {
                yield return t;
            }
        }
    }

    public Option<T, Message> GetSymbol<T>(Identifier name) where T : Symbol
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
        for (var scope = this; scope is not null; scope = scope._parentScope) {
            if (scope._symbolTable.TryGetValue(name, out symbol)) {
                return true;
            }
        }

        symbol = default;
        return false;
    }

    public bool TryGetSymbol<T>(Identifier name, [NotNullWhen(true)] out T? symbol) where T : Symbol => TryGetSymbol(name.ToString(), out symbol);
    public bool TryGetSymbol(Identifier name, [NotNullWhen(true)] out Symbol? symbol) => TryGetSymbol(name.ToString(), out symbol);

    public bool HasSymbol(Identifier name) => HasSymbol(name.ToString());
    public bool HasSymbol(string name) => _symbolTable.ContainsKey(name);
}
