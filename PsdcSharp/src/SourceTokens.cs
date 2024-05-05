using System.Collections;
using Scover.Psdc.Tokenization;

namespace Scover.Psdc;

class SourceTokens : IEnumerable<Token>
{
    readonly Lazy<Range> _inputRange;
    readonly IEnumerable<Token> _tokens;
    public SourceTokens(IEnumerable<Token> tokens, int count)
    {
        Count = count;
        _tokens = tokens.Take(count);
        _inputRange = count == 0
            ? new(Range.EndAt(Index.Start))
            : new(() => {
                var lastSourceToken = _tokens.Last();
                return _tokens.First().StartIndex..(lastSourceToken.StartIndex + lastSourceToken.Length);
            });
    }

    public static SourceTokens Empty { get; } = new([], 0);

    public int Count { get; }

    public Range InputRange => _inputRange.Value;

    public IEnumerator<Token> GetEnumerator() => _tokens.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_tokens).GetEnumerator();
}
