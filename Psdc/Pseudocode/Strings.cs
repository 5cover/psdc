using System.Text;

namespace Scover.Psdc.Pseudocode;

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

    public static StringBuilder Escape(ReadOnlySpan<char> input, IFormatProvider? fmtProvider = null)
    {
        StringBuilder o = new();

        // The goal here is to escape a string as to provide a friendly, printable rep

        foreach (var c in input) {
            switch (c) {
            case '\'': o.Append("\\'"); break;
            case '\"': o.Append("\\\""); break;
            case '\\': o.Append(@"\\"); break;
            case '\a': o.Append("\\a"); break;
            case '\b': o.Append("\\b"); break;
            case '\f': o.Append("\\f"); break;
            case '\n': o.Append("\\n"); break;
            case '\r': o.Append("\\r"); break;
            case '\t': o.Append("\\t"); break;
            case '\v': o.Append("\\v"); break;
            case '\x1b': o.Append("\\e"); break;
            case { } when char.IsControl(c):
                // If c can be represented in 3 octal chars
                if (c < 8 * 8 * 8) {
                    o.Append(fmtProvider, $"\\{Convert.ToString(c, 8)}");
                } else {
                    o.Append(fmtProvider, $"\\u{(int)c:X4}");
                }
                break;
            default: o.Append(c); break;
            }
        }

        return o;
    }
}
