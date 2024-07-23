namespace Scover.Psdc.Library;

static class OptionCollectionExtensions
{
    /// <summary>
    /// Accumulates either all the values, or the errors from a collection of <see cref="Option{T, TError}"/> instances.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <typeparam name="TError">The type of the error.</typeparam>
    /// <param name="options">The collection of <see cref="Option{T, TError}"/> instances.</param>
    /// <returns>An <see cref="Option{T, TError}"/> instance containing either the accumulated values or errors.</returns>
    public static Option<IEnumerable<T>, IEnumerable<TError>> Accumulate<T, TError>(this IEnumerable<Option<T, TError>> options)
    {
        List<T> values = [];
        List<TError> errors = [];

        // Make sure to only iterate the input sequence once.
        foreach (var option in options) {
            if (option.HasValue) {
                values.Add(option.Value);
            } else {
                errors.Add(option.Error);
            }
        }

        return errors.Count > 0
            ? errors.None<IEnumerable<T>, IEnumerable<TError>>()
            : values.Some<IEnumerable<T>, IEnumerable<TError>>();
    }

    public static Option<T> ElementAtOrNone<T>(this IEnumerable<T> source, int index)
    {
        if (index >= 0) {
            if (source is IReadOnlyList<T> list) {
                if (index < list.Count) {
                    return list[index].Some();
                }
            } else {
                using var enumerator = source.GetEnumerator();
                while (enumerator.MoveNext()) {
                    if (index-- == 0) {
                        return enumerator.Current.Some();
                    }
                }
            }
        }
        return Option.None<T>();
    }

    public static Option<T> FirstOrNone<T>(this IEnumerable<Option<T>> source, Func<Option<T>, bool> predicate)
     => source.FirstOrDefault(predicate, Option.None<T>());

    public static Option<T> FirstOrNone<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        foreach (var element in source) {
            if (predicate(element)) {
                return element.Some();
            }
        }
        return Option.None<T>();
    }

    public static Option<T> FirstOrNone<T>(this IEnumerable<T> source)
    {
        if (source is IReadOnlyList<T> list) {
            if (list.Count > 0) {
                return list[0].Some();
            }
        } else {
            using var enumerator = source.GetEnumerator();
            if (enumerator.MoveNext()) {
                return enumerator.Current.Some();
            }
        }
        return Option.None<T>();
    }

    public static Option<T> FirstSome<T>(this IEnumerable<Option<T>> source)
    {
        foreach (var item in source) {
            if (item.HasValue) {
                return item;
            }
        }
        return Option.None<T>();
    }
}
