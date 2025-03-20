using Scover.Psdc.Lexing;

namespace Scover.Psdc;

public sealed class SourceTokens
{
    readonly Lazy<Range> _location;
    public SourceTokens(IEnumerable<Token> tokens, int count)
    {
        Count = count;
        tokens = tokens.Take(count);
        _location = count == 0
            ? new(Range.EndAt(Index.Start))
            : new(() => tokens.First().Position.Start..tokens.Last().Position.End);
    }

    public SourceTokens(Token token) : this(token.Yield(), 1) { }

    public static SourceTokens Empty { get; } = new([], 0);

    public int Count { get; }

    public Range Location => _location.Value;
}
