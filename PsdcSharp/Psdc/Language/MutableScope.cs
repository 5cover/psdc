using System.Diagnostics.CodeAnalysis;

using Scover.Psdc.Messages;

namespace Scover.Psdc.Language;

public sealed class MutableScope(Scope? scope) : Scope(scope)
{
    public void AddOrError(Messenger messenger, Symbol symbol)
    {
        if (!TryAdd(symbol, out var existingSymbol)) {
            messenger.Report(Message.ErrorRedefinedSymbol(symbol, existingSymbol));
        }
    }

    public bool TryAdd(Symbol symbol) => _symbolTable.TryAdd(symbol.Name.ToString(), symbol);

    public bool TryAdd(Symbol symbol, [NotNullWhen(false)] out Symbol? existingSymbol)
    {
        var added = _symbolTable.TryAdd(symbol.Name.ToString(), symbol);
        existingSymbol = added ? null : _symbolTable[symbol.Name.ToString()];
        return added;
    }
}
