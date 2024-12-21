using Scover.Psdc.Lexing;

namespace Scover.Psdc;

public sealed class SourceTokens
{
    readonly Lazy<Range> _location;
    readonly IEnumerable<Token> _tokens;

    public SourceTokens(IEnumerable<Token> tokens, int count)
    {
        Count = count;
        _tokens = tokens.Take(count);
        _location = count == 0
            ? new(Range.EndAt(Index.Start))
            : new(() => _tokens.First().Position.Start.._tokens.Last().Position.End);
    }

    public SourceTokens(Token token) : this(token.Yield(), 1) { }

    public static SourceTokens Empty { get; } = new([], 0);

    public int Count { get; }

    public Range Location => _location.Value;
}
