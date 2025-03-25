using System.Collections.Immutable;

namespace Scover.Psdc.Parsing;

static class ParseResult
{
    public static ParseResult<T> Ok<T>(ImmutableStack<string> subject, int read, ImmutableArray<ParseError> errors, T value) =>
        new ParseResultImpl<T>(subject, read, errors, true, value);

    public static ParseResult<T> Ko<T>(ImmutableStack<string> subject, int read, ImmutableArray<ParseError> errors) =>
        new ParseResultImpl<T>(subject, read, errors, false, default);

    readonly record struct ParseResultImpl<T>(
        ImmutableStack<string> Subject,
        int Read,
        ImmutableArray<ParseError> Errors,
        bool HasValue,
        T? Value
    ) : ParseResult<T>;
}
interface ParseResult<out T> : Option<T>
{
    ImmutableStack<string> Subject { get; }
    int Read { get; }
    ImmutableArray<ParseError> Errors { get; }
}
