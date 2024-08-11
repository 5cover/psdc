using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Scover.Psdc.Messages;
using Scover.Psdc.Tokenization;

namespace Scover.Psdc.Library;

readonly record struct Position(int Line, int Column)
{
    public override string ToString() => $"L {Line + 1}, col {Column + 1}";
}

static class Extensions
{
    public static void CheckKeys<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary,
        IReadOnlyCollection<TKey> keys,
        Func<TKey, TValue> fallbackValueSelector,
        Action<TKey> onExcessKey)
    {
        foreach (var key in dictionary.Keys) {
            if (!keys.Contains(key)) {
                onExcessKey(key);
            }
        }
        foreach (var key in keys) {
            _ = dictionary.TryAdd(key, fallbackValueSelector(key));
        }
    }
    public static (TKey, TValue) ToTuple<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp)
     => (kvp.Key, kvp.Value);

    /// <summary>
    /// Get the flattened index of a multidimensional array.
    /// </summary>
    /// <param name="dimensionIndexes">The 0-based index in each dimension.</param>
    /// <param name="dimensionLengths">The length of each dimension.</param>
    /// <returns>A flattened index that indexes a 1-dimensional array whose length is the product of <paramref name="dimensionLengths"/>.</returns>
    /// <remarks><paramref name="dimensionIndexes"/> and <paramref name="dimensionLengths"/> must have the same count.</remarks>
    public static int FlatIndex(this IEnumerable<int> dimensionIndexes, IEnumerable<int> dimensionLengths)
    {
        Debug.Assert(dimensionIndexes.Count() == dimensionLengths.Count());
        return dimensionLengths
            .Zip(dimensionIndexes)
            .Aggregate(0, (soFar, next) => soFar * next.First + next.Second);
    }
    public static T Product<T>(this IEnumerable<T> source) where T : IMultiplyOperators<T, T, T>
     => source.Aggregate((soFar, next) => soFar *= next);

    public static bool AllSemanticsEqual<T>(this IEnumerable<T> first, IEnumerable<T> second) where T : EquatableSemantics<T>
     => first.AllZipped(second, (f, s) => f.SemanticsEqual(s));

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

    public static int DigitCount(this int n, int @base = 10) => n == 0 ? 1 : 1 + (int)Math.Log(n, @base);

    public static ValueOption<int> IndexOf<T>(this IReadOnlyList<T> list, T item)
    {
        for (int i = 0; i < list.Count; ++i) {
            if (EqualityComparer<T>.Default.Equals(list[i], item)) {
                return i;
            }
        }
        return default;
    }

    public static void DoInColor(this (ConsoleColor? foreground, ConsoleColor? background) color, Action action)
    {
        color.SetColor();
        action();
        Console.ResetColor();
    }

    public static (ConsoleColor? foreground, ConsoleColor? background) GetConsoleColor(this MessageSeverity msgSeverity) => msgSeverity switch {
        MessageSeverity.Error => (ConsoleColor.Red, null),
        MessageSeverity.Warning => (ConsoleColor.Yellow, null),
        MessageSeverity.Suggestion => (ConsoleColor.Blue, null),
        _ => throw msgSeverity.ToUnmatchedException(),
    };

    public static IEnumerator<T> GetGenericEnumerator<T>(this T[] array) => ((IEnumerable<T>)array).GetEnumerator();

    public static int GetLeadingWhitespaceCount(this ReadOnlySpan<char> str)
    {
        var count = 0;
        var enumerator = str.GetEnumerator();
        while (enumerator.MoveNext() && char.IsWhiteSpace(enumerator.Current)) {
            count++;
        }

        return count;
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

    /// <summary>
    /// Checks if an exception is exogenous and could have been thrown by the filesystem API.
    /// </summary>
    /// <returns>
    /// <para>
    /// <see langword="true"/> if <paramref name="e"/> is of or derived from any of the following types :
    /// </para>
    /// <br><see cref="IOException"/></br><br><see cref="UnauthorizedAccessException"/></br><br><see
    /// cref="SecurityException"/></br>
    /// <para>Otherwise; <see langword="false"/>.</para>
    /// </returns>
    /// <remarks>Note that unrelated methods may throw any of these exceptions.</remarks>
    public static bool IsFileSystemExogenous(this Exception e)
        => e is IOException or UnauthorizedAccessException or System.Security.SecurityException;

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

    public static Option<bool> NextIsOfType(this IEnumerable<Token> tokens, params TokenType[] types)
         => tokens.FirstOrNone().Map(token => types.Contains(token.Type));

    /// <summary>Asserts that <paramref name="t" /> isn't <see langword="null" />.</summary>
    /// <remarks>This is a safer replacement for the null-forgiving operator (<c>!</c>).</remarks>
    /// <returns><paramref name="t" />, not null.</returns>
    public static T NotNull<T>([NotNull] this T? t, string? message = null)
    {
        Debug.Assert(t is not null, message);
        return t;
    }

    public static bool OptionSemanticsEqual<T>(this Option<T> first, Option<T> second) where T : EquatableSemantics<T>
     => first.HasValue && second.HasValue
        ? first.Value.SemanticsEqual(second.Value)
        : first.HasValue == second.HasValue;

    public static void SetColor(this (ConsoleColor? foreground, ConsoleColor? background) color)
    {
        if (color.foreground is { } fg) {
            Console.ForegroundColor = fg;
        }
        if (color.background is { } bg) {
            Console.BackgroundColor = bg;
        }
    }

    public static UnreachableException ToUnmatchedException<T>(this T t) => new($"Unmatched {typeof(T).Name}: {t}");

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

    public static IEnumerable<T> Yield<T>(this T t)
    {
        yield return t;
    }
}

static class Function
{
    /// <summary>
    /// Compose an action that executes the specified action for each item in the given collection.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="action">The action to be executed for each item.</param>
    /// <returns>An action that can be used to iterate over a collection and execute the specified action for each item.</returns>
    public static Action<IEnumerable<T>> Foreach<T>(this Action<T> action)
         => items => {
             foreach (var item in items) {
                 action(item);
             }
         };
}
