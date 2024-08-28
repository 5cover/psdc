using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;

namespace Scover.Psdc.Library;

public static class Extensions
{
    internal static (TKey, TValue) ToTuple<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp)
     => (kvp.Key, kvp.Value);

    /// <summary>
    /// Get the flattened index of a multi-dimensional array.
    /// </summary>
    /// <param name="dimensionIndexes">The 0-based index in each dimension.</param>
    /// <param name="dimensionLengths">The length of each dimension.</param>
    /// <returns>An integer that indexes a 1-dimensional array whose length is the product of <paramref name="dimensionLengths"/>.</returns>
    internal static int FlatIndex(this IEnumerable<int> dimensionIndexes, IEnumerable<int> dimensionLengths)
    {
        Debug.Assert(dimensionIndexes.Count() == dimensionLengths.Count());
        return dimensionLengths
            .Zip(dimensionIndexes)
            .Aggregate(0, (soFar, next) => soFar * next.First + next.Second);
    }

    /// <summary>
    /// Get the N-dimensional index in a multi-dimensional array from the flattened index.
    /// </summary>
    /// <param name="flatIndex">The flattened index.</param>
    /// <param name="dimensionLengths">The length of each dimension.</param>
    /// <remarks>This function is the inverse of <see cref="FlatIndex"/>.</remarks>
    /// <returns>An enumarable containing 0-based index in each dimension. Same count as <paramref name="dimensionLengths"/>.</returns>
    internal static IEnumerable<int> NDimIndex(this int flatIndex, IReadOnlyList<int> dimensionLengths)
     => dimensionLengths.Reverse().Select(l => {
         flatIndex = Math.DivRem(flatIndex, l, out var i);
         return i;
     }).Reverse();

    internal static T Product<T>(this IEnumerable<T> source) where T : IMultiplyOperators<T, T, T>
     => source.Aggregate((soFar, next) => soFar *= next);

    internal static bool AllSemanticsEqual<T>(this IEnumerable<T> first, IEnumerable<T> second) where T : EquatableSemantics<T>
     => first.AllZipped(second, (f, s) => f.SemanticsEqual(s));

    internal static bool AllZipped<T1, T2>(this IEnumerable<T1> first, IEnumerable<T2> second, Func<T1, T2, bool> predicate)
    {
        using var e1 = first.GetEnumerator();
        using var e2 = second.GetEnumerator();

        bool all = true;
        while (all && e1.MoveNext()) {
            all = e2.MoveNext() && predicate(e1.Current, e2.Current);
        }
        return !e2.MoveNext() && all;
    }

    internal static int DigitCount(this int n, int @base = 10) => n == 0 ? 1 : 1 + (int)Math.Log(n, @base);

    internal static ValueOption<int> IndexOfFirst<T>(this IReadOnlyList<T> list, T item)
    {
        for (int i = 0; i < list.Count; ++i) {
            if (EqualityComparer<T>.Default.Equals(list[i], item)) {
                return i;
            }
        }
        return default;
    }

    internal static ValueOption<int> IndexOfFirst<T>(this IReadOnlyList<T> list, Predicate<T> predicate)
    {
        for (int i = 0; i < list.Count; ++i) {
            if (predicate(list[i])) {
                return i;
            }
        }
        return default;
    }

    internal static bool Indexes(this int index, int length, int @base = 0) => @base <= index && index < length + @base;

    internal static ValueOption<int> SomeIndexes(this int index, int length, int @base = 0) => index.Indexes(length, @base)
        ? (index - @base).Some()
        : default;

    internal static IEnumerator<T> GetGenericEnumerator<T>(this T[] array) => ((IEnumerable<T>)array).GetEnumerator();

    internal static int GetLeadingWhitespaceCount(this ReadOnlySpan<char> str)
    {
        var count = 0;
        var enumerator = str.GetEnumerator();
        while (enumerator.MoveNext() && char.IsWhiteSpace(enumerator.Current)) {
            count++;
        }

        return count;
    }

    internal static ReadOnlySpan<char> GetLine(this string str, int lineNumber)
    {
        var lines = str.AsSpan().EnumerateLines();
        bool success = true;
        for (; success && lineNumber >= 0; --lineNumber) {
            success = lines.MoveNext();
        }
        return success ? lines.Current : throw new ArgumentOutOfRangeException(nameof(lineNumber), "Line number is out of range");
    }

    internal static Position GetPositionAt(this string str, Index index)
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
    internal static bool IsFileSystemExogenous(this Exception e)
        => e is IOException or UnauthorizedAccessException or System.Security.SecurityException;

    internal static T LogOperation<T>(this string name, bool verbose, Func<T> operation)
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

    internal static string? ToStringFmt(this object obj, IFormatProvider? fmtProvider) => obj switch {
        IFormattable f => f.ToString(null, fmtProvider),
        IConvertible c => c.ToString(fmtProvider),
        _ => obj.ToString()
    };

    internal static bool OptionSemanticsEqual<T>(this Option<T> first, Option<T> second) where T : EquatableSemantics<T>
     => first.HasValue && second.HasValue
        ? first.Value.SemanticsEqual(second.Value)
        : first.HasValue == second.HasValue;

    internal static UnreachableException ToUnmatchedException<T>(this T t)
     => new(string.Create(CultureInfo.InvariantCulture, $"Unmatched {typeof(T).Name}: {t}"));

    internal static void WriteNTimes(this TextWriter writer, int n, char c)
    {
        if (n < 0) {
            throw new ArgumentOutOfRangeException(nameof(n), "cannot be negative");
        }
        while (n > 0) {
            n--;
            writer.Write(c);
        }
    }

    internal static IEnumerable<T> Yield<T>(this T t)
    {
        yield return t;
    }

    public static bool TryGetAt<T>(this IEnumerable<T> source, int index, [NotNullWhen(true)] out T? item) where T : notnull
    {
        if (index >= 0) {
            if (source is IReadOnlyList<T> rolist) {
                if (index < rolist.Count) {
                    item = rolist[index];
                    return true;
                }
            } else if (source is IList<T> list) {
                if (index < list.Count) {
                    item = list[index];
                    return true;
                }
            } else {
                using var enumerator = source.GetEnumerator();
                while (enumerator.MoveNext()) {
                    if (index-- == 0) {
                        item = enumerator.Current;
                        return true;
                    }
                }
            }
        }
        item = default;
        return false;
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
    internal static Action<IEnumerable<T>> Foreach<T>(this Action<T> action)
         => items => {
             foreach (var item in items) {
                 action(item);
             }
         };
}

static class Set
{
    internal static HashSet<T> Of<T>(params T[] items) => [.. items];
}
