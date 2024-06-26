using System.Diagnostics.CodeAnalysis;

using Scover.Psdc.Messages;
using Scover.Psdc.Parsing;

namespace Scover.Psdc.Language;

interface ReadOnlyScope
{
    public IReadOnlyDictionary<Identifier, Symbol> Symbols { get; }

    public Option<T, Message> GetSymbol<T>(Identifier name) where T : Symbol;

    public bool TryGetSymbol<T>(Identifier name, [NotNullWhen(true)] out T? symbol) where T : Symbol;
}
