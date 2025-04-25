using System.Collections.Immutable;
using Scover.Psdc.Lexing;

namespace Scover.Psdc.Parsing;

sealed class ParseError(ImmutableStack<string> subject, int index, List<IReadOnlyCollection<TokenType>> expected)
{
    public ImmutableStack<string> Subject { get; } = subject;
    public int Index { get; } = index;
    public List<IReadOnlyCollection<TokenType>> Expected { get; } = expected;
}
