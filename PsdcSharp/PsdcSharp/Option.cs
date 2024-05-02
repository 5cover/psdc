using System.Diagnostics.CodeAnalysis;

namespace Scover.Psdc;

// Covariant Option monad

internal interface Option<out T>
{
    [MemberNotNullWhen(true, nameof(Value))]
    bool HasValue { get; }

    T? Value { get; }
}

internal interface Option<out T, out TError>
{
    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    bool HasValue { get; }

    T? Value { get; }
    TError? Error { get; }
}

internal static class Option
{
    public static Option<T> DiscardError<T, TError>(this Option<T, TError> option)
     => new OptionImpl<T>(option.HasValue, option.Value);

    public static Option<T> MatchError<T, TError>(this Option<T, TError> option, Action<TError> actionWithError)
    {
        if (!option.HasValue) {
            actionWithError.Invoke(option.Error);
        }
        return new OptionImpl<T>(option.HasValue, option.Value);
    }

    public static Option<T, TNewError> MapError<T, TError, TNewError>(this Option<T, TError> option, Func<TError, TNewError> errorMap)
     => new OptionImpl<T, TNewError>(option.HasValue, option.Value, option.HasValue ? default : errorMap(option.Error));

    public static T ValueOr<T>(this Option<T> option, T defaultValue)
     => option.HasValue ? option.Value : defaultValue;
    public static T ValueOr<T>(this Option<T> option, Func<T> defaultValue)
     => option.HasValue ? option.Value : defaultValue();
    public static T ValueOr<T, TError>(this Option<T, TError> option, T defaultValue)
     => option.HasValue ? option.Value : defaultValue;
    public static T ValueOr<T, TError>(this Option<T, TError> option, Func<T> defaultValue)
     => option.HasValue ? option.Value : defaultValue();

    public static Option<T> Some<T>(this T value)
     => new OptionImpl<T>(true, value);
    public static Option<T, TError> Some<T, TError>(this T value)
     => new OptionImpl<T, TError>(true, value, default);

    public static Option<T> SomeNotNull<T>(this T? value) where T : class
     => value is not null ? value.Some() : None<T>();
    public static Option<T, TError> SomeNotNull<T, TError>(this T? value, TError error) where T : class
    => value is not null ? value.Some<T, TError>() : None<T, TError>(error);
    public static Option<T, TError> SomeNotNull<T, TError>(this T? value, Func<TError> error) where T : class
     => value is not null ? value.Some<T, TError>() : None<T, TError>(error());

    public static Option<T> None<T>() => new OptionImpl<T>(false, default);
    public static Option<T, TError> None<T, TError>(this TError error) => new OptionImpl<T, TError>(false, default, error);

    public static Option<TResult> Map<T, TResult>(this Option<T> option, Func<T, TResult> transform)
     => option.HasValue ? transform(option.Value).Some() : None<TResult>();
    public static Option<TResult> Map<T1, T2, TResult>(this Option<(T1, T2)> option, Func<T1, T2, TResult> transform)
     => option.HasValue ? transform(option.Value.Item1, option.Value.Item2).Some() : None<TResult>();
    public static Option<TResult, TError> Map<T, TError, TResult>(this Option<T, TError> option, Func<T, TResult> transform)
     => option.HasValue
        ? transform(option.Value).Some<TResult, TError>()
        : None<TResult, TError>(option.Error);

    public static Option<TResult> FlatMap<T, TResult>(this Option<T> option, Func<T, Option<TResult>> transform)
     => option.HasValue ? transform(option.Value) : None<TResult>();
    public static Option<TResult, TError> FlatMap<T, TError, TResult>(this Option<T, TError> option,
        Func<T, Option<TResult, TError>> transform)
     => option.HasValue ? transform(option.Value) : None<TResult, TError>(option.Error);

    public static Option<TResult, TError> FlatMap<T1, T2, TError, TResult>(this Option<(T1, T2), TError> option,
        Func<T1, T2, Option<TResult, TError>> transform)
        => option.HasValue ? transform(option.Value.Item1, option.Value.Item2) : None<TResult, TError>(option.Error);

    public static Option<T> Else<T>(this Option<T> original, Option<T> other)
     => original.HasValue
        ? original
        : other;

    public static Option<TResult, TError> OrWithError<TResult, TError>(this Option<TResult> option, TError error)
     => new OptionImpl<TResult, TError>(option.HasValue, option.Value, option.HasValue ? default : error);

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
    public static void MatchSome<T1, T2>(this Option<(T1, T2)> option, Action<T1, T2> action)
    {
        if (option.HasValue) {
            action(option.Value.Item1, option.Value.Item2);
        }
    }

    public static Option<(T1, T2)> Combine<T1, T2>(this Option<T1> option1, Option<T2> option2)
     => (option1.HasValue && option2.HasValue)
        ? Some((option1.Value, option2.Value))
        : None<(T1, T2)>();

    public static Option<(T1, T2), TError> Combine<T1, T2, TError>(this Option<T1, TError> option1, Option<T2, TError> option2)
     => (option1.HasValue && option2.HasValue)
        ? Some<(T1, T2), TError>((option1.Value, option2.Value))
        : None<(T1, T2), TError>(option1.Error ?? option2.Error.NotNull());

    private readonly record struct OptionImpl<T>(bool HasValue, T? Value) : Option<T>;

    private readonly record struct OptionImpl<T, TError>(bool HasValue, T? Value, TError? Error) : Option<T, TError>;
}

internal static class OptionalCollectionExtensions
{
    /// <summary>
    /// Accumulates the values and errors from a collection of <see cref="Option{T, TError}"/> instances.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <typeparam name="TError">The type of the error.</typeparam>
    /// <param name="options">The collection of <see cref="Option{T, TError}"/> instances.</param>
    /// <returns>An <see cref="Option{T, TError}"/> instance containing either the accumulated values or errors.</returns>
    public static Option<IEnumerable<T>, IEnumerable<TError>> Accumulate<T, TError>(this IEnumerable<Option<T, TError>> options)
    {
        var values = options.Where(o => o.HasValue).Select(o => o.Value!);
        var errors = options.Where(o => !o.HasValue).Select(o => o.Error!);

        return errors.Any()
            ? errors.None<IEnumerable<T>, IEnumerable<TError>>()
            : values.Some<IEnumerable<T>, IEnumerable<TError>>();
    }

    public static Option<IEnumerable<T>> AccumulateAtLeast1<T>(this IEnumerable<Option<T>> options)
    {
        var somes = options.Accumulate();
        return somes.Any() ? somes.Some() : Option.None<IEnumerable<T>>();
    }

    public static IEnumerable<T> Accumulate<T>(this IEnumerable<Option<T>> options)
     => options.Where(v => v.HasValue).Select(v => v.Value!);
    public static T FirstSome<T>(this IEnumerable<Option<T>> options)
     => options.First(v => v.HasValue).Value!;

    public static Option<T> FirstOrNone<T>(this IEnumerable<Option<T>> options, Func<Option<T>, bool> predicate)
     => options.FirstOrDefault(predicate, Option.None<T>());
    public static Option<T> FirstOrNone<T>(this IEnumerable<T> items, Func<T, bool> predicate)
     => items.FirstOrDefault(predicate) is { } first ? first.Some() : Option.None<T>();
    public static Option<T> FirstOrNone<T>(this IEnumerable<T> items)
     => items.FirstOrDefault() is { } first ? first.Some() : Option.None<T>();
    public static Option<T> LastOrNone<T>(this IEnumerable<Option<T>> options, Func<Option<T>, bool> predicate)
     => options.LastOrDefault(predicate, Option.None<T>());
    public static Option<T> LastOrNone<T>(this IEnumerable<T> items, Func<T, bool> predicate)
     => items.LastOrDefault(predicate) is { } last ? last.Some() : Option.None<T>();
    public static Option<T> LastOrNone<T>(this IEnumerable<T> items)
     => items.LastOrDefault() is { } last ? last.Some() : Option.None<T>();
}
