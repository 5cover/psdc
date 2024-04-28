
using Scover.Psdc.Tokenization;

namespace Scover.Psdc.Parsing;

internal interface ParseResult<out T> : Option<T, ParseError>
{
    Partition<Token> SourceTokens { get; }
    ParseResult<T> WithSourceTokens(Partition<Token> newSourceTokens);
}

internal sealed record ParseError(Option<Token> ErroneousToken, IReadOnlySet<TokenType> ExpectedTokens, string ExpectedProductionName)
{

    public static ParseError Create<T>(Option<Token> erroneousToken, IEnumerable<TokenType> expectedTokens)
     => new(erroneousToken, expectedTokens.ToHashSet(), typeof(T).Name.ToLower());
    public static ParseError Create<T>(Option<Token> erroneousToken, TokenType expectedTokens)
     => new(erroneousToken, new HashSet<TokenType> { expectedTokens }, typeof(T).Name.ToLower());

    public bool IsEquivalent(ParseError? other) => other is not null && other.ExpectedTokens.SetEquals(ExpectedTokens);
}

internal static class ParseResult
{
    public static ParseResult<T> Ok<T>(T result) where T : Node
     => new ParseResultImpl<T>(true, result, null, result.SourceTokens);
    public static ParseResult<T> Ok<T>(Partition<Token> sourceTokens, T result)
     => new ParseResultImpl<T>(true, result, null, sourceTokens);

    public static ParseResult<T> Fail<T>(Partition<Token> sourceTokens, ParseError error)
     => new ParseResultImpl<T>(false, default, error, sourceTokens);

    public static ParseResult<T> Else<T>(this ParseResult<T> original, Func<ParseResult<T>> alternative)
    {
        if (original.HasValue) {
            return original;
        }
        ParseResult<T> alt = alternative();
        return new ParseResultImpl<T>(alt.HasValue, alt.Value, alt.Error?.CombineWith<T>(original.Error), alt.SourceTokens);
    }

    public static ParseResult<TResult> Map<T, TResult>(this ParseResult<T> original, Func<Partition<Token>, T, TResult> transform)
     => original.HasValue
        ? Ok(original.SourceTokens, transform(original.SourceTokens, original.Value))
        : Fail<TResult>(original.SourceTokens, original.Error);

    public static ParseResult<TResult> FlatMap<T, TResult>(this ParseResult<T> pr, Func<T, ParseResult<TResult>> transform)
     => pr.HasValue ? transform(pr.Value) : Fail<TResult>(pr.SourceTokens, pr.Error);

    private static ParseError CombineWith<T>(this ParseError original, ParseError @new)
     => new(original.ErroneousToken.Else(@new.ErroneousToken),
            original.ExpectedTokens.Concat(@new.ExpectedTokens).ToHashSet(), typeof(T).Name.ToLower());

    private sealed record ParseResultImpl<T>(
        bool HasValue,
        T? Value,
        ParseError? Error,
        Partition<Token> SourceTokens) : ParseResult<T>
    {
        public ParseResult<T> WithSourceTokens(Partition<Token> newSourceTokens)
         => this with { SourceTokens = newSourceTokens };
    }
}
