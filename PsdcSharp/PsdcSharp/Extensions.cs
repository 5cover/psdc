using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Scover.Psdc.Parsing;

namespace Scover.Psdc;

internal readonly record struct Position(int Line, int Column)
{
    public override string ToString() => $"L {Line + 1}, col {Column + 1}";
}

internal static class Parse
{
    public static Option<int> ToInt32(this string s)
     => int.TryParse(s, out int res) ? res.Some() : Option.None<int>();
}

internal static class Extensions
{
    public static string RemoveDiacritics(this string text)
     => new string(text.Normalize(NormalizationForm.FormD)
        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
        .ToArray()).Normalize(NormalizationForm.FormC);

    public static Position GetPositionAt(this string str, Index index)
    {
        int line = 0, column = 0;
        for (int i = 0; i < index.Value; i++) {
            if (str.AsSpan().Slice(i, Environment.NewLine.Length).SequenceEqual(Environment.NewLine)) {
                line++;
                column = 0;
                i += Environment.NewLine.Length - 1; // Skip the rest of the newline sequence
            } else {
                column++;
            }
        }
        return new Position(line, column);
    }

    public static (ConsoleColor? foreground, ConsoleColor? background) GetConsoleColor(this MessageSeverity msgSeverity) => msgSeverity switch {
        MessageSeverity.Error => (ConsoleColor.Red, null),
        MessageSeverity.Warning => (ConsoleColor.Yellow, null),
        MessageSeverity.Suggestion => (ConsoleColor.Blue, null),
        _ => throw msgSeverity.ToUnmatchedException(),
    };

    public static IEnumerable<T> Yield<T>(this T t)
    {
        yield return t;
    }

    public static UnreachableException ToUnmatchedException<T>(this T t) => new($"Unmatched {typeof(T).Name}: {t}");

    public static void DoInColor(this (ConsoleColor? foreground, ConsoleColor? background) color, Action action)
    {
        color.SetColor();
        action();
        Console.ResetColor();
    }

    public static void SetColor(this (ConsoleColor? foreground, ConsoleColor? background) color)
    {
        if (color.foreground is { } fg) {
            Console.ForegroundColor = fg;
        }
        if (color.background is { } bg) {
            Console.BackgroundColor = bg;
        }
    }

    public static T LogOperation<T>(this string name, bool verbose, Func<T> operation)
    {
        if (verbose) {
            Console.Error.Write($"{name}...");
        }
        T t = operation();
        if (verbose) {
            Console.Error.WriteLine(" done");
        }
        return t;
    }

    /// <summary>Asserts that <paramref name="t" /> isn't <see langword="null" />.</summary>
    /// <remarks>This is a safer replacement for the null-forgiving operator (<c>!</c>).</remarks>
    /// <returns><paramref name="t" />, not null.</returns>
    public static T NotNull<T>([NotNull] this T? t, string? message = null)
    {
        Debug.Assert(t is not null, message);
        return t;
    }

    public static ReadOnlySpan<char> GetLine(this string str, int lineNumber)
    {
        var lines = str.AsSpan().EnumerateLines();
        bool success = true;
        for (; success && lineNumber >= 0; --lineNumber) {
            success = lines.MoveNext();
        }
        return success ? lines.Current : throw new ArgumentOutOfRangeException(nameof(lineNumber), "Line number is out of range");
    }

    public static int GetLeadingWhitespaceCount(this ReadOnlySpan<char> str)
    {
        var count = 0;
        var enumerator = str.GetEnumerator();
        while (enumerator.MoveNext() && char.IsWhiteSpace(enumerator.Current)) {
            count++;
        }

        return count;
    }

    public static IReadOnlyDictionary<TKey, TValue> UnionCombineDuplicates<TKey, TValue>(
        this IReadOnlyDictionary<TKey, TValue> d1,
        IReadOnlyDictionary<TKey, TValue> d2,
        Func<TValue, TValue, TValue> combineValuesHavingSameKey)
        where TKey : notnull
    {
        Dictionary<TKey, TValue> ret = new(d1);

        foreach (var (key, value) in d2) {
            ret[key] = ret.TryGetValue(key, out var d1Value)
                ? combineValuesHavingSameKey(d1Value, value)
                : value;
        }

        return ret;
    }

    public static int GetSequenceHashCode<T>(this IEnumerable<T> items)
    {
        HashCode hc = new();
        foreach (T t in items) {
            hc.Add(t);
        }
        return hc.ToHashCode();
    }

    public static bool AllZipped<T1, T2>(this IEnumerable<T1> first, IEnumerable<T2> second, Func<T1, T2, bool> predicate)
    {
        using var e1 = first.GetEnumerator();
        using var e2 = second.GetEnumerator();

        bool all = true;
        while (all && e1.MoveNext()) {
            all = e2.MoveNext() && predicate(e1.Current, e2.Current);
        }
        return !e2.MoveNext() && all;
    }

    public static bool AllSemanticsEqual<T>(this IEnumerable<T> first, IEnumerable<T> second) where T : EquatableSemantics<T>
     => first.AllZipped(second, (f, s) => f.SemanticsEqual(s));

    public static bool OptionSemanticsEqual(this Option<Node> first, Option<Node> second)
     => first.HasValue && second.HasValue
        ? first.Value.SemanticsEqual(second.Value)
        : first.HasValue == second.HasValue;

    public static void WriteNTimes(this TextWriter writer, int n, char c)
    {
        if (n < 0) {
            throw new ArgumentOutOfRangeException(nameof(n), "cannot be negative");
        }
        while (n > 0) {
            n--;
            writer.Write(c);
        }
    }

    public static IEnumerator<T> GetGenericEnumerator<T>(this T[] array) => ((IEnumerable<T>)array).GetEnumerator();
}
