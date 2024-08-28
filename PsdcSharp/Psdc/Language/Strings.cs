using System.Globalization;
using System.Text;

namespace Scover.Psdc.Language;

public enum EscapeMode
{
    ForString,
    ForChar
};

static class Strings
{
    const int LengthU32 = 8;
    const int LengthU16 = 4;
    const int MaxLengthOctal = 3;
    const int MaxLengthHex = sizeof(ulong) * 2;

    const int OffsetOctal = 1;
    const int OffsetHex = 2;
    const int OffsetU16 = 2;
    const int OffsetU32 = 2;
    const int OffsetSimple = 2;

    public static StringBuilder Escape(ReadOnlySpan<char> input, EscapeMode mode, IFormatProvider? fmtProvider = null)
    {
        StringBuilder o = new();

        // The goal here is to escape a string as to provide a friendly, printable rep

        foreach (var c in input) {
            switch (c) {
            case '\'' when mode is EscapeMode.ForChar:
                o.Append("\\'");
                break;
            case '\"' when mode is EscapeMode.ForString:
                o.Append("\\\"");
                break;
            case '\\':
                o.Append("\\\\");
                break;
            case '\a':
                o.Append("\\a");
                break;
            case '\b':
                o.Append("\\b");
                break;
            case '\f':
                o.Append("\\f");
                break;
            case '\n':
                o.Append("\\n");
                break;
            case '\r':
                o.Append("\\r");
                break;
            case '\t':
                o.Append("\\t");
                break;
            case '\v':
                o.Append("\\v");
                break;
            case char when char.IsControl(c):
                // If c can be represented in 3 octal chars
                if (c < 8 * 8 * 8) {
                    o.Append(fmtProvider, $"\\{Convert.ToString(c, 8)}");
                } else {
                    o.Append(fmtProvider, $"\\u{(int)c:X4}");
                }
                break;
            default:
                o.Append(c);
                break;
            }
        }

        return o;
    }

    public static StringBuilder Unescape(ReadOnlySpan<char> input, EscapeMode mode, IFormatProvider? fmtProvider = null)
    {
        StringBuilder o = new();
        int i = 0;

        while (i < input.Length) {
            if (i + 1 < input.Length && input[i] == '\\') {
                switch (input[i + 1]) {
                case '\'' when mode is EscapeMode.ForChar:
                    o.Append('\'');
                    i += OffsetSimple;
                    break;
                case '\"' when mode is EscapeMode.ForString:
                    o.Append('\"');
                    i += OffsetSimple;
                    break;
                case '\\':
                    o.Append('\\');
                    i += OffsetSimple;
                    break;
                case 'a':
                    o.Append('\a');
                    i += OffsetSimple;
                    break;
                case 'b':
                    o.Append('\b');
                    i += OffsetSimple;
                    break;
                case 'f':
                    o.Append('\f');
                    i += OffsetSimple;
                    break;
                case 'n':
                    o.Append('\n');
                    i += OffsetSimple;
                    break;
                case 'r':
                    o.Append('\r');
                    i += OffsetSimple;
                    break;
                case 't':
                    o.Append('\t');
                    i += OffsetSimple;
                    break;
                case 'v':
                    o.Append('\v');
                    i += OffsetSimple;
                    break;
                case char c when GetOctalDigitValue(c) is { HasValue: true } firstDigit: {
                    ushort val = firstDigit.Value;
                    int digitCount = 1;
                    for (; digitCount < MaxLengthOctal
                        && GetOctalDigitValue(input, i + OffsetOctal + digitCount) is { HasValue: true } digit;
                        ++digitCount) {
                        val *= 8;
                        val += digit.Value;
                    }
                    o.Append((char)val);
                    i += OffsetOctal + digitCount;
                    break;
                }
                case 'x' when GetHexDigitValue(input, i + OffsetHex) is { HasValue: true } firstDigit: {
                    ulong val = firstDigit.Value;
                    int digitCount = 1;
                    for (; GetHexDigitValue(input, i + OffsetHex + digitCount) is { HasValue: true } digit; ++digitCount) {
                        if (digitCount < MaxLengthHex) {
                            val *= 16;
                            val += digit.Value;
                        }
                    }
                    o.Append((char)val);
                    i += OffsetHex + digitCount;
                    break;
                }
                case 'u' when i + OffsetU16 + LengthU16 <= input.Length
                           && ushort.TryParse(input.Slice(i + OffsetU16, LengthU16), NumberStyles.AllowHexSpecifier, fmtProvider, out var codepoint):
                    o.Append((char)codepoint);
                    i += OffsetU16 + LengthU16;
                    break;
                case 'U' when i + OffsetU32 + LengthU32 <= input.Length
                           && uint.TryParse(input.Slice(i + OffsetU32, LengthU32), NumberStyles.AllowHexSpecifier, fmtProvider, out var codepoint):
                    o.Append((char)codepoint);
                    i += OffsetU32 + LengthU32;
                    break;
                default:
                    // invalid escape sequences are simply treated as regular characters.
                    o.Append(input[i]);
                    ++i;
                    break;
                }
            } else {
                o.Append(input[i]);
                ++i;
            }
        }

        return o;
    }

    static ValueOption<byte> GetOctalDigitValue(char c) => c is >= '0' and <= '7'
        ? (byte)(c - '0')
        : Option.None<byte>();
    static ValueOption<byte> GetOctalDigitValue(ReadOnlySpan<char> s, int i)
     => i < s.Length && s[i] is >= '0' and <= '7'
        ? (byte)(s[i] - '0')
        : Option.None<byte>();
    static ValueOption<byte> GetHexDigitValue(ReadOnlySpan<char> s, int i)
     => i < s.Length ? Option.None<byte>() : s[i] switch {
         >= '0' and <= '9' => (byte)(s[i] - '0'),
         >= 'A' and <= 'F' => (byte)(s[i] - 'A' + 10),
         >= 'a' and <= 'f' => (byte)(s[i] - 'a' + 10),
         _ => default
     };
}

