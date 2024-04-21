using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Scover.Psdc.Tokenization;

namespace Scover.Psdc;

public readonly record struct Position(int Line, int Column)
{
    public override string ToString() => $"L {Line + 1}, col {Column + 1}";
}

public readonly record struct Partition<T>(IEnumerable<T> Source, int Count) : IEnumerable<T>
{
    public Partition(params T[] values) : this(values, values.Length)
    {
    }

    private IEnumerable<T> Values => Source.Take(Count);

    public IEnumerator<T> GetEnumerator() => Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Values).GetEnumerator();
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

    public static Position GetPositionAt(this string str, int index)
    {
        int line = 0, column = 0;
        for (int i = 0; i < index; i++) {
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

    public static T LogOperation<T>(this string name, Func<T> operation)
    {
        Console.Error.Write($"{name}...");
        T t = operation();
        Console.Error.WriteLine(" done");
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

    public static void Raise(this EventHandler eventHandler, object sender)
     => eventHandler.Invoke(sender, EventArgs.Empty);

    public static int GetSequenceHashCode<T>(this IEnumerable<T> items)
    {
        HashCode hc = new();
        foreach (T t in items) {
            hc.Add(t);
        }
        return hc.ToHashCode();
    }

    public static IEnumerable<TResult> ZipStrict<TFirst, TSecond, TResult>(
        this IEnumerable<TFirst> first,
        IEnumerable<TSecond> second,
        Func<TFirst, TSecond, TResult> resultSelector)
    {
        using var e1 = first.GetEnumerator();
        using var e2 = second.GetEnumerator();
        while (e1.MoveNext()) {
            yield return e2.MoveNext()
                ? resultSelector(e1.Current, e2.Current)
                : throw new InvalidOperationException("Sequences differed in length");
        }
        if (e2.MoveNext()) {
            throw new InvalidOperationException("Sequences differed in length");
        }
    }

    public static string GetSourceCode(this IEnumerable<Token> sourceTokens, string input)
    {
        var lastSourceToken = sourceTokens.Last();
        return input[sourceTokens.First().StartIndex..(lastSourceToken.StartIndex + lastSourceToken.Length)];
    }
}

