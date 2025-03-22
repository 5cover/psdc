using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

using Scover.Psdc.Messages;

namespace Scover.Psdc.Lexing;

public sealed partial class Lexer(Messenger msger)
{
    const int NaIndex = -1;

    readonly Messenger _msger = msger;

    readonly record struct LineCont(int Index, int Length);

    readonly Queue<LineCont> _sortedLineConts = new();

    char[] _input = [];
    int _i;
    int _start;
    int _strayStart;
    int _lineContOffsetBeforeStart;
    int _lineContOffsetSinceStart;

    static Lexer() => Debug.Assert(reservedWords.Keys
#pragma warning disable CA1862
       .All(k => k.ToLower(Format.Code) == k && k == IdentifierRegex().Match(k).Value));
#pragma warning restore CA1862

    bool IsAtEnd => _i >= _input.Length;
    ReadOnlySpan<char> Lexeme => _input.AsSpan()[_start.._i];
    FixedRange LexemePos => new(_lineContOffsetBeforeStart + _start, _lineContOffsetBeforeStart + _lineContOffsetSinceStart + _i);

    public IEnumerable<Token> Lex(string input)
    {
        _input = PreprocessLineContinuations(input);
        _strayStart = NaIndex;

        _i = 0;
        _start = 0;
        _lineContOffsetBeforeStart = 0;
        _lineContOffsetSinceStart = 0;

        while (_sortedLineConts.Count > 0 && _sortedLineConts.Peek().Index == 0) {
            _lineContOffsetBeforeStart += _sortedLineConts.Dequeue().Length;
        }

        while (!IsAtEnd) {
            switch (Advance()) {
            case var c when char.IsWhiteSpace(c):
                ReportInvalidToken();
                break;
            case '{':
                yield return Ok(TokenType.LBrace);
                break;
            case '[':
                yield return Ok(TokenType.LBracket);
                break;
            case '(':
                yield return Ok(TokenType.LParen);
                break;
            case '}':
                yield return Ok(TokenType.RBrace);
                break;
            case ']':
                yield return Ok(TokenType.RBracket);
                break;
            case ')':
                yield return Ok(TokenType.RParen);
                break;
            case '=':
                yield return Ok(
                    Match('>') ? TokenType.Arrow :
                    Match('=') ? TokenType.Eq :
                    TokenType.Equal);
                break;
            case ':':
                yield return Ok(Match('=') ? TokenType.ColonEqual : TokenType.Colon);
                break;
            case ',':
                yield return Ok(TokenType.Comma);
                break;
            case '#':
                yield return Ok(TokenType.Hash);
                break;
            case '/':
                if (Match('/')) {
                    while (!IsAtEnd && Advance() != '\n') ;
                } else if (Match('*')) {
                    while (!IsAtEnd && (Advance() != '*' || !Match('/'))) ;
                } else {
                    yield return Ok(TokenType.Div);
                }
                break;
            case '.':
                yield return Match(char.IsAsciiDigit) ? Real() : Ok(TokenType.Dot);
                break;
            case ';':
                yield return Ok(TokenType.Semi);
                break;
            case '>':
                yield return Ok(Match('=') ? TokenType.Ge : TokenType.Gt);
                break;
            case '<':
                yield return Ok(Match('=') ? TokenType.Le : TokenType.Lt);
                break;
            case '-':
                yield return Ok(TokenType.Minus);
                break;
            case '%':
                yield return Ok(TokenType.Mod);
                break;
            case '*':
                yield return Ok(TokenType.Mul);
                break;
            case '!' when Match('='):
                yield return Ok(TokenType.Neq);
                break;
            case '+':
                yield return Ok(TokenType.Plus);
                break;
            case '\'': {
                var value = EscapedString('\'');
                if (value is null) {
                    _msger.Report(Message.ErrorUnterminatedCharLiteral(LexemePos));
                    break;
                }
                switch (value.Length) {
                case 0:
                    _msger.Report(Message.ErrorCharLitEmpty(LexemePos));
                    break;
                case 1:
                    yield return Ok(TokenType.LiteralChar, value[0]);
                    break;
                default:
                    _msger.Report(Message.ErrorCharLitContainsMoreThanOneChar(LexemePos, value[0]));
                    break;
                }
                break;
            }
            case '\"': {
                var value = EscapedString('\"');
                if (value is null) {
                    _msger.Report(Message.ErrorUnterminatedStringLiteral(LexemePos));
                    break;
                }
                yield return Ok(TokenType.LiteralString, value.ToString());
                break;
            }
            case var c when char.IsAsciiDigit(c): {
                yield return Number();
                break;
            }
            default: {
                var inputSpan = _input.AsSpan();
                var matches = IdentifierRegex().EnumerateMatches(inputSpan, _start);
                if (matches.MoveNext()) {
                    var m = matches.Current;
                    Debug.Assert(_start == m.Index);
                    if (m.Length > 1) Advance(m.Length - 1);
                    var word = inputSpan.Slice(m.Index, m.Length).ToString();
                    yield return reservedWords.TryGetValue(word.ToLower(Format.Code), out var type)
                        ? Ok(type)
                        : Ok(TokenType.Ident, word);
                    break;
                }

                if (_strayStart == NaIndex) _strayStart = _start;
                break;
            }
            }

            _start = _i;
            _lineContOffsetBeforeStart += _lineContOffsetSinceStart;
            _lineContOffsetSinceStart = 0;
        }

        while (_sortedLineConts.TryDequeue(out var lc)) {
            _lineContOffsetBeforeStart += lc.Length;
        }

        yield return Ok(TokenType.Eof);
    }

    static ValueOption<byte> HexDigitValue(char c) => c switch {
        >= '0' and <= '9' => (byte)(c - '0'),
        >= 'A' and <= 'F' => (byte)(c - 'A' + 10),
        >= 'a' and <= 'f' => (byte)(c - 'a' + 10),
        _ => Option.None<byte>(),
    };

    [GeneratedRegex(@"\G[\p{L}_][\p{L}_0-9]*")]
    private static partial Regex IdentifierRegex();

    static ValueOption<byte> OctDigitValue(char c) => c is >= '0' and <= '7'
        ? (byte)(c - '0')
        : Option.None<byte>();

    char[] PreprocessLineContinuations(string input)
    {
        _sortedLineConts.Clear();
        if (input.Length == 0) return [];

        var preprocessedCode = new char[input.Length];

        int i = 0, j = 0;
        while (j < input.Length - 1) {
            if (input[j] == '\\' && char.IsWhiteSpace(input[j + 1])) {
                int start = j;
                while (++j < input.Length && input[j] != '\n' && char.IsWhiteSpace(input[j])) ;
                if (j < input.Length && input[j] == '\n') {
                    _sortedLineConts.Enqueue(new(i, ++j - start));
                    continue;
                }
                j = start;
            }
            preprocessedCode[i++] = input[j++];
        }
        if (j < input.Length) preprocessedCode[i++] = input[j];
        Array.Resize(ref preprocessedCode, i);
        return preprocessedCode;
    }

    char Advance(int of = 1)
    {
        char c = _input[_i];
        _i += of;
        LineCont lc;
        while (_sortedLineConts.Count > 0 && _sortedLineConts.Peek().Index < _i) {
            _lineContOffsetSinceStart += _sortedLineConts.Dequeue().Length;
        }
        return c;
    }

    StringBuilder? EscapedString(char endDelimiter)
    {
        StringBuilder value = new();

        bool inEscape = false;
        while (!IsAtEnd && (inEscape || _input[_i] != endDelimiter) && _input[_i] is not '\n' and not '\r') {
            char c = Advance();
            if (inEscape) {
                inEscape = false;
                Unescape(c, value);
            } else if (c == '\\') {
                inEscape = true;
            } else {
                value.Append(c);
            }
        }
        return Match(endDelimiter) ? value : null;
    }

    bool Match(char expected)
    {
        if (IsAtEnd || _input[_i] != expected) return false;
        Advance();
        return true;
    }

    ValueOption<T> Match<T>(Func<char, ValueOption<T>> f) => IsAtEnd
        ? default
        : f(_input[_i]).Tap(_ => Advance());

    bool Match(Func<char, bool> f)
    {
        if (IsAtEnd || !f(_input[_i])) return false;
        Advance();
        return true;
    }

    Token Number()
    {
        Debug.Assert(char.IsAsciiDigit(_input[_i - 1]));
        while (Match(char.IsAsciiDigit)) ;
        return Match('.') && Match(char.IsAsciiDigit)
            ? Real()
            : Ok(TokenType.LiteralInt, long.Parse(Lexeme, Format.Code));
    }

    Token Ok(TokenType type, object? value = null)
    {
        ReportInvalidToken();
        return new(LexemePos, type, value);
    }

    Token Real()
    {
        Debug.Assert(char.IsAsciiDigit(_input[_i - 1]));
        while (Match(char.IsAsciiDigit)) ;
        return Ok(TokenType.LiteralReal, decimal.Parse(Lexeme, Format.Code));
    }

    void ReportInvalidToken()
    {
        if (_strayStart == NaIndex) return;
        _msger.Report(Message.ErrorUnknownToken(new(_strayStart, _start)));
        _strayStart = NaIndex;
    }

    void Unescape(char c, StringBuilder value)
    {
        const int MaxLengthOctal = 3;
        const int MaxLengthU16 = 4;
        const int MaxLengthU32 = 8;
        switch (c) {
        case '\'' or '\"' or '\\':
            value.Append(c);
            break;
        case 'a':
            value.Append('\a');
            break;
        case 'b':
            value.Append('\b');
            break;
        case 'f':
            value.Append('\f');
            break;
        case 'n':
            value.Append('\n');
            break;
        case 'r':
            value.Append('\r');
            break;
        case 't':
            value.Append('\t');
            break;
        case 'v':
            value.Append('\v');
            break;
        case 'e':
            value.Append('\x1b');
            break;
        case 'x': {
            var hexDigit = Match(HexDigitValue);
            if (!hexDigit.HasValue) {
                _msger.Report(Message.ErrorInvalidEscapeSequence(LexemePos, 'x',
                    "must be followed by at least 1 hexadecimal digit"));
                break;
            }
            int val = hexDigit.Value;
            for (int nDigits = 1;
                nDigits < MaxLengthU32 && (hexDigit = Match(HexDigitValue)).HasValue;
                nDigits++) {
                val *= 16;
                val += hexDigit.Value;
            }
            AppendSafeConvertFromUtf32(value, val);
            break;
        }
        case 'u': {
            ushort val = 0;
            ushort nDigits = 0;
            for (;
                nDigits < MaxLengthU16
             && Match(HexDigitValue) is { HasValue: true } hexDigit;
                nDigits++) {
                val *= 16;
                val += hexDigit.Value;
            }
            if (nDigits < MaxLengthU16) {
                _msger.Report(Message.ErrorInvalidEscapeSequence(LexemePos, 'u',
                    $"must be followed by {MaxLengthU16} hexadecimal digits"));
            } else {
                value.Append((char)val);
            }
            break;
        }
        case 'U': {
            int val = 0;
            int nDigits = 0;
            for (;
                nDigits < MaxLengthU32
             && Match(HexDigitValue) is { HasValue: true } hexDigit;
                nDigits++) {
                val *= 16;
                val += hexDigit.Value;
            }
            if (nDigits < MaxLengthU32) {
                _msger.Report(Message.ErrorInvalidEscapeSequence(LexemePos, 'u',
                    $"must be followed by {MaxLengthU32} hexadecimal digits"));
            } else {
                AppendSafeConvertFromUtf32(value, val);
            }
            break;
        }
        case var _ when OctDigitValue(c) is { HasValue: true } octDigit: {
            ushort val = octDigit.Value;
            for (int nDigits = 1;
                nDigits < MaxLengthOctal && (octDigit = Match(OctDigitValue)).HasValue;
                nDigits++) {
                val *= 8;
                val += octDigit.Value;
            }
            Debug.Assert(val < 512);
            value.Append((char)val);
            break;
        }
        default:
            value.Append(c);
            _msger.Report(Message.ErrorInvalidEscapeSequence(LexemePos, c));
            break;
        }
    }
    static StringBuilder AppendSafeConvertFromUtf32(StringBuilder sb, int val) => ((uint)val - 0x110000u ^ 0xD800u) >= 0xFFEF0800u // IsValidUnicodeScalar
        ? sb.Append(char.ConvertFromUtf32(val))
        : sb.Append((char)val);

    static readonly Dictionary<string, TokenType> reservedWords = new() {
        ["tableau"] = TokenType.Array,
        ["début"] = TokenType.Begin,
        ["debut"] = TokenType.Begin,
        ["booléen"] = TokenType.Boolean,
        ["booleen"] = TokenType.Boolean,
        ["caractère"] = TokenType.Character,
        ["caractere"] = TokenType.Character,
        ["constante"] = TokenType.Constant,
        ["faire"] = TokenType.Do,
        ["sinon"] = TokenType.Else,
        ["sinon_si"] = TokenType.ElseIf,
        ["fin"] = TokenType.End,
        ["fin_pour"] = TokenType.EndFor,
        ["fin_si"] = TokenType.EndIf,
        ["fin_selon"] = TokenType.EndSwitch,
        ["fin_tant_que"] = TokenType.EndWhile,
        ["faux"] = TokenType.False,
        ["pour"] = TokenType.For,
        ["fonction"] = TokenType.Function,
        ["si"] = TokenType.If,
        ["entier"] = TokenType.Integer,
        ["sortie"] = TokenType.Out,
        ["procédure"] = TokenType.Procedure,
        ["procedure"] = TokenType.Procedure,
        ["programme"] = TokenType.Program,
        ["lire"] = TokenType.Read,
        ["réel"] = TokenType.Real,
        ["reel"] = TokenType.Real,
        ["retourne"] = TokenType.Return,
        ["délivre"] = TokenType.Returns,
        ["delivre"] = TokenType.Returns,
        ["chaîne"] = TokenType.String,
        ["chaine"] = TokenType.String,
        ["structure"] = TokenType.Structure,
        ["selon"] = TokenType.Switch,
        ["alors"] = TokenType.Then,
        ["vrai"] = TokenType.True,
        ["ent"] = TokenType.Trunc,
        ["type"] = TokenType.Type,
        ["quand"] = TokenType.When,
        ["quand_autre"] = TokenType.WhenOther,
        ["tant_que"] = TokenType.While,
        ["écrire"] = TokenType.Write,
        ["ecrire"] = TokenType.Write,
        ["et"] = TokenType.And,
        ["non"] = TokenType.Not,
        ["or"] = TokenType.Or,
        ["xor"] = TokenType.Xor,
    };

}
