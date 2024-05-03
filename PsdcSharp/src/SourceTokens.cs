using System.Collections;
using Scover.Psdc.Tokenization;

namespace Scover.Psdc;

internal class SourceTokens : IEnumerable<Token>
{
    private readonly Lazy<string> _sourceCode;
    private readonly IEnumerable<Token> _tokens;
    public SourceTokens(IEnumerable<Token> tokens, int count)
    {
        Count = count;
        _tokens = tokens.Take(count);
        _sourceCode = new(() => {
            var lastSourceToken = _tokens.Last();
            return Globals.Input[_tokens.First().StartIndex..(lastSourceToken.StartIndex + lastSourceToken.Length)];
        });
    }

    public static SourceTokens Empty { get; } = new([], 0);

    public int Count { get; }

    public string SourceCode => _sourceCode.Value;

    public IEnumerator<Token> GetEnumerator() => _tokens.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_tokens).GetEnumerator();
}
