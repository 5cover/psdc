using System.Diagnostics.CodeAnalysis;
using Scover.Psdc.Parsing;

namespace Scover.Psdc.StaticAnalysis;

internal interface ReadOnlyScope
{
    public IReadOnlyDictionary<Identifier, Symbol> Symbols { get; }
    public bool TryGetSymbol<T>(Identifier name, [NotNullWhen(true)] out T? symbol) where T : Symbol;

    public Option<T, Message> GetSymbol<T>(Identifier name) where T : Symbol;

    public bool TryGetSymbolOrError<T>(Messenger messenger, Identifier name, out T? symbol) where T : Symbol;
}
