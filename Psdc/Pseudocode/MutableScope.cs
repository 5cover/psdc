using System.Diagnostics.CodeAnalysis;

using Scover.Psdc.Messages;

namespace Scover.Psdc.Pseudocode;

public sealed class MutableScope(Scope? scope) : Scope(scope)
{
    public void AddOrError(Messenger messenger, Symbol symbol)
    {
        if (!TryAdd(symbol, out var existingSymbol)) {
            messenger.Report(Message.ErrorRedefinedSymbol(symbol, existingSymbol));
        }
    }

    public bool TryAdd(Symbol symbol) => SymbolTable.TryAdd(symbol.Name.ToString(), symbol);

    public bool TryAdd(Symbol symbol, [NotNullWhen(false)] out Symbol? existingSymbol)
    {
        var added = SymbolTable.TryAdd(symbol.Name.ToString(), symbol);
        existingSymbol = added ? null : SymbolTable[symbol.Name.ToString()];
        return added;
    }
}
