using System.Diagnostics.CodeAnalysis;

namespace Scover.Psdc;

// Covariant Option monad

interface Option<out T>
{
    [MemberNotNullWhen(true, nameof(Value))]
    bool HasValue { get; }

    T? Value { get; }

    /// <summary>Returns an <see cref="ValueOption{T}"/> as an <see cref="Option{T}"/></summary>
    /// <remarks>Used to circumvent implicit casting limitations.</remarks>
    public static Option<T> Of(ValueOption<T> option) => option;
}

interface Option<out T, out TError>
{
    TError? Error { get; }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    bool HasValue { get; }

    T? Value { get; }

    /// <summary>Returns an <see cref="ValueOption{T, TError}"/> as an <see cref="Option{T, TError}"/></summary>
    /// <remarks>Used to circumvent implicit casting limitations.</remarks>
    public static Option<T, TError> Of(ValueOption<T, TError> option) => option;
}

readonly record struct ValueOption<T>(bool HasValue, T? Value) : Option<T>
{
    public static implicit operator ValueOption<T>(T value) => new(true, value);
}

readonly record struct ValueOption<T, TError>(bool HasValue, T? Value, TError? Error) : Option<T, TError>
{
    public static implicit operator ValueOption<T, TError>(T value) => new(true, value, default);
    public static implicit operator ValueOption<T, TError>(TError error) => new(false, default, error);
}

static class Option
{
    public static T Unwrap<T>(this Option<T> option)
     => option.HasValue ? option.Value : throw new InvalidOperationException("Option has no value");

    public static Option<(T1, T2)> Combine<T1, T2>(this Option<T1> option1, Option<T2> option2)
     => (option1.HasValue && option2.HasValue)
        ? (option1.Value, option2.Value).Some()
        : None<(T1, T2)>();

    public static Option<(T1, T2), TError> Combine<T1, T2, TError>(this Option<T1, TError> option1, Option<T2, TError> option2)
     => (option1.HasValue && option2.HasValue)
        ? Some<(T1, T2), TError>((option1.Value, option2.Value))
        : None<(T1, T2), TError>(option1.HasValue ? option2.Error.NotNull() : option1.Error);

    public static Option<T, TError> OrWithError<T, TError>(this Option<T> option, TError error)
     => option.HasValue ? option.Value.Some<T, TError>() : error.None<T, TError>();

    public static Option<T> DiscardError<T, TError>(this Option<T, TError> option)
         => new ValueOption<T>(option.HasValue, option.Value);

    public static Option<TResult> FlatCast<T, TResult>(this Option<T> option)
     => option.HasValue && option.Value is TResult result ? result.Some() : None<TResult>();

    public static Option<TResult> FlatMap<T, TResult>(this Option<T> option, Func<T, Option<TResult>> transform)
     => option.HasValue ? transform(option.Value) : None<TResult>();

    public static Option<TResult, TError> FlatMap<T, TError, TResult>(this Option<T, TError> option,
        Func<T, Option<TResult, TError>> transform)
     => option.HasValue ? transform(option.Value) : None<TResult, TError>(option.Error);

    public static Option<TResult, TError> FlatMap<T1, T2, TError, TResult>(this Option<(T1, T2), TError> option,
        Func<T1, T2, Option<TResult, TError>> transform)
        => option.HasValue ? transform(option.Value.Item1, option.Value.Item2) : None<TResult, TError>(option.Error);

    public static Option<TResult> Map<T1, T2, TResult>(this Option<(T1, T2)> option, Func<T1, T2, TResult> transform)
     => option.HasValue ? transform(option.Value.Item1, option.Value.Item2).Some() : None<TResult>();

    public static Option<TResult> Map<T, TResult>(this Option<T> option, Func<T, TResult> transform)
     => option.HasValue ? transform(option.Value).Some() : None<TResult>();

    public static Option<TResult, TError> Map<T, TError, TResult>(this Option<T, TError> option, Func<T, TResult> transform)
     => option.HasValue
        ? transform(option.Value).Some<TResult, TError>()
        : None<TResult, TError>(option.Error);

    public static Option<T, TNewError> MapError<T, TError, TNewError>(this Option<T, TError> option, Func<TError, TNewError> errorMap)
     => new ValueOption<T, TNewError>(option.HasValue, option.Value, option.HasValue ? default : errorMap(option.Error));

    public static void Match<T>(this Option<T> option, Action<T> some, Action none)
    {
        if (option.HasValue) {
            some(option.Value);
        } else {
            none();
        }
    }

    public static void Match<T1, T2>(this Option<(T1, T2)> option, Action<T1, T2> some, Action none)
    {
        if (option.HasValue) {
            some(option.Value.Item1, option.Value.Item2);
        } else {
            none();
        }
    }

    public static void Match<T, TError>(this Option<T, TError> option, Action<T> some, Action<TError> none)
    {
        if (option.HasValue) {
            some(option.Value);
        } else {
            none(option.Error);
        }
    }

    public static TResult Match<T, TResult>(this Option<T> option, Func<T, TResult> some, Func<TResult> none)
     => option.HasValue ? some(option.Value) : none();

    public static TResult Match<T, TError, TResult>(this Option<T, TError> option, Func<T, TResult> some, Func<TError, TResult> none)
     => option.HasValue ? some(option.Value) : none(option.Error);

    public static Option<T> MatchError<T, TError>(this Option<T, TError> option, Action<TError> actionWithError)
    {
        if (!option.HasValue) {
            actionWithError.Invoke(option.Error);
        }
        return new ValueOption<T>(option.HasValue, option.Value);
    }

    public static void MatchSome<T, TError>(this Option<T, TError> option, Action<T> action)
    {
        if (option.HasValue) {
            action(option.Value);
        }
    }

    public static void MatchSome<T>(this Option<T> option, Action<T> action)
    {
        if (option.HasValue) {
            action(option.Value);
        }
    }

    public static Option<T> None<T>() => new ValueOption<T>(false, default);

    public static Option<T, TError> None<T, TError>(this TError error) => new ValueOption<T, TError>(false, default, error);

    public static Option<T> Some<T>(this T value)
     => new ValueOption<T>(true, value);

    public static Option<T, TError> Some<T, TError>(this T value)
     => new ValueOption<T, TError>(true, value, default);

    public static Option<T> SomeNotNull<T>(this T? value) where T : class
     => value is not null ? value.Some() : None<T>();

    public static Option<T, TError> SomeNotNull<T, TError>(this T? value, TError error) where T : class
    => value is not null ? value.Some<T, TError>() : None<T, TError>(error);

    public static T ValueOr<T>(this Option<T> option, T defaultValue)
                                     => option.HasValue ? option.Value : defaultValue;

    public static T ValueOr<T, TError>(this Option<T, TError> option, T defaultValue)
     => option.HasValue ? option.Value : defaultValue;
}

static class OptionalCollectionExtensions
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
}
