namespace Scover.Psdc.Parsing;

public interface ParseResult<out T> : Option<T, ParseError>
{
    SourceTokens SourceTokens { get; }

    ParseResult<T> WithSourceTokens(SourceTokens newSourceTokens);
}

static class ParseResult
{
    public static ParseResult<T> Fail<T>(SourceTokens sourceTokens, ParseError error)
     => new ParseResultImpl<T>(false, default, error, sourceTokens);

    public static ParseResult<TResult> FlatMap<T, TResult>(this ParseResult<T> pr, Func<T, ParseResult<TResult>> transform)
     => pr.HasValue ? transform(pr.Value) : Fail<TResult>(pr.SourceTokens, pr.Error);

    public static ParseResult<TResult> Map<T, TResult>(this ParseResult<T> original, Func<SourceTokens, T, TResult> transform)
     => original.HasValue
        ? Ok(original.SourceTokens, transform(original.SourceTokens, original.Value))
        : Fail<TResult>(original.SourceTokens, original.Error);

    public static ParseResult<T> MapError<T>(this ParseResult<T> result, Func<ParseError, ParseError> transformError)
     => result.HasValue
        ? result
        : Fail<T>(result.SourceTokens, transformError(result.Error));

    public static ParseResult<T> Ok<T>(SourceTokens sourceTokens, T result)
     => new ParseResultImpl<T>(true, result, null, sourceTokens);

    sealed record ParseResultImpl<T>(
        bool HasValue,
        T? Value,
        ParseError? Error,
        SourceTokens SourceTokens) : ParseResult<T>
    {
        public ParseResult<T> WithSourceTokens(SourceTokens newSourceTokens)
         => this with { SourceTokens = newSourceTokens };
    }
}
