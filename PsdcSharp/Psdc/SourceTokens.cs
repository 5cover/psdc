using System.Collections;

using Scover.Psdc.Lexing;

namespace Scover.Psdc;

public class SourceTokens : IEnumerable<Token>
{
    readonly Lazy<Range> _inputRange;
    readonly IEnumerable<Token> _tokens;

    public SourceTokens(IEnumerable<Token> tokens, int count)
    {
        Count = count;
        _tokens = tokens.Take(count);
        _inputRange = count == 0
            ? new(Range.EndAt(Index.Start))
            : new(() => _tokens.First().Position.Start.._tokens.Last().Position.End);
    }

    public static SourceTokens Empty { get; } = new([], 0);

    public int Count { get; }

    public Range InputRange => _inputRange.Value;

    public IEnumerator<Token> GetEnumerator() => _tokens.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_tokens).GetEnumerator();
}
