using System.Text;

namespace Scover.Psdc.Pseudocode;

public static class Strings
{
    public static string WrapInCode(this string input)
    {
        // Find the longest run of backticks in the input
        int maxBacktickSeq = 0;
        int currentCount = 0;

        foreach (char c in input) {
            if (c == '`') {
                currentCount++;
                if (currentCount > maxBacktickSeq) maxBacktickSeq = currentCount;
            } else {
                currentCount = 0;
            }
        }

        // Use one more backtick than the maximum in the string
        string wrapper = new('`', maxBacktickSeq + 1);

        // Determine if padding is necessary (starts or ends with a backtick)
        bool needsPadding = input.StartsWith('`') || input.EndsWith('`');

        return needsPadding ? $"{wrapper} {input} {wrapper}" : $"{wrapper}{input}{wrapper}";
    }

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
