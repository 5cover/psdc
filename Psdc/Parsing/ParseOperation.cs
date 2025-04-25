using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Scover.Psdc.Lexing;

namespace Scover.Psdc.Parsing;

readonly record struct ParsingContext(ImmutableArray<Token> Tokens, int Start, ImmutableStack<string> Subject)
{
    public ParsingContext PushSubject(string subject) => this with { Subject = Subject.Push(subject) };
}

delegate ParseResult<T> Parser<out T>(ParsingContext ctx);

// Naming conventions
//  Try* -> method never emit errors

sealed class ParseOperation
{
    readonly ParsingContext _ctx;
    int _i;
    readonly ImmutableArray<ParseError>.Builder _errors = ImmutableArray.CreateBuilder<ParseError>();

    ParseOperation(ParsingContext ctx)
    {
        _ctx = ctx;
        _i = ctx.Start;
    }

    public static ParseOperation Start(ParsingContext ctx) => new(ctx);

    public void One(TokenType type)
    {
        if (Peek.Type != type) {
            AddError([type]);
            return;
        }
        Advance();
    }

    public bool Switch<T>(
        IReadOnlyDictionary<TokenType, Parser<T>> dispatch,
        [NotNullWhen(true)] out T? result)
    {
        if (dispatch.TryGetValue(Peek.Type, out var parser)) {
            var r = Call(parser);
            result = r.Value;
            return r.HasValue;
        }
        AddError(dispatch.Keys.ToArray());
        result = default;
        return false;
    }

    public bool SwitchConsume<T>(
        IReadOnlyDictionary<TokenType, Parser<T>> dispatch,
        [NotNullWhen(true)] out T? result)
    {
        if (dispatch.TryGetValue(Peek.Type, out var parser)) {
            Advance();
            var r = Call(parser);
            result = r.Value;
            return r.HasValue;
        }
        AddError(dispatch.Keys.ToArray());
        result = default;
        return false;
    }

    public bool One(TokenType type, [NotNullWhen(true)] out Token token)
    {
        if (Peek.Type != type) {
            AddError([type]);
            token = default;
            return false;
        }
        token = Advance();
        return true;
    }

    public bool One(IImmutableSet<TokenType> types, [NotNullWhen(true)] out Token token)
    {
        if (!types.Contains(Peek.Type)) {
            AddError(types);
            token = default;
            return false;
        }
        token = Advance();
        return true;
    }

    public bool One<T>(Parser<T> parser, [NotNullWhen(true)] out T? result)
    {
        var r = Call(parser);
        result = r.Value;
        return r.HasValue;
    }

    /// <summary>Try to execute a parser. Unlike <see cref="One{T}"/>, no error can be emitted from this function.</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="parser"></param>
    /// <returns></returns>
    public Option<T> TryOne<T>(Parser<T> parser)
    {
        var r = parser(_ctx with { Start = _i });
        if (r.HasValue) _i += r.Read;
        return r;
    }

    public void ZeroOrMore<T>(Parser<T> parser, TokenType endAt, IReadOnlyDictionary<TokenType, bool> syncPoints, out ImmutableArray<T> result)
    {
        var items = ImmutableArray.CreateBuilder<T>();
        while (!IsAtEnd && Peek.Type != endAt) {
            var r = Call(parser);
            if (r.HasValue) items.Add(r.Value);
            if (!r.HasValue || !r.Errors.IsEmpty) Synchronize(syncPoints);
        }
        result = items.ToImmutable();
    }

    public ParseResult<T> Ko<T>() => ParseResult.Ko<T>(_ctx.Subject, _i - _ctx.Start, _errors.ToImmutable());

    public ParseResult<T> Ok<T>(T value) => ParseResult.Ok(_ctx.Subject, _i - _ctx.Start, _errors.ToImmutable(), value);

    public FixedRange Extent => new(_ctx.Start, _i);

    ParseResult<T> Call<T>(Parser<T> parser)
    {
        var r = parser(_ctx with { Start = _i });
        _i += r.Read;
        _errors.AddRange(r.Errors);
        return r;
    }

    void Synchronize(IReadOnlyDictionary<TokenType, bool> syncPoints)
    {
        bool consume = false;
        while (!IsAtEnd && !syncPoints.TryGetValue(Peek.Type, out consume)) _i++;
        if (!IsAtEnd && consume) _i++;
    }

    ParseError? _lastError;
    void AddError(IReadOnlyCollection<TokenType> expected)
    {
        if (_lastError is not null && _lastError.Index == _i) {
            _lastError.Expected.Add(expected);
        } else {
            _errors.Add(_lastError = new(_ctx.Subject, _i, [expected]));
        }
    }

    bool IsAtEnd => Peek.Type == TokenType.Eof;
    Token Advance() => _ctx.Tokens[_i++];
    Token Peek => _ctx.Tokens[_i];
}
