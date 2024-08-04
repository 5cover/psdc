using System.Diagnostics.CodeAnalysis;

using Scover.Psdc.Messages;
using Scover.Psdc.Parsing;

namespace Scover.Psdc.Language;

interface ReadOnlyScope
{
    public Option<T, Message> GetSymbol<T>(Identifier name) where T : Symbol;

    public bool TryGetSymbol<T>(Identifier name, [NotNullWhen(true)] out T? symbol) where T : Symbol;
    public bool TryGetSymbol<T>(string name, [NotNullWhen(true)] out T? symbol) where T : Symbol;

    public bool TryGetSymbol(Identifier name, [NotNullWhen(true)] out Symbol? symbol);
    public bool TryGetSymbol(string name, [NotNullWhen(true)] out Symbol? symbol);

    public bool HasSymbol(Identifier name);
    public bool HasSymbol(string name);
}
