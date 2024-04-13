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
    public static Option<T, TError> None<T, TError>(TError error) => new OptionImpl<T, TError>(false, default, error);

    public static Option<TResult> Map<T, TResult>(this Option<T> option, Func<T, TResult> transform)
     => option.HasValue ? transform(option.Value).Some() : None<TResult>();
    public static Option<TResult, TError> Map<T, TError, TResult>(this Option<T, TError> option, Func<T, TResult> transform)
     => option.HasValue
        ? transform(option.Value).Some<TResult, TError>()
        : None<TResult, TError>(option.Error);

    public static Option<TResult> FlatMap<T, TResult>(this Option<T> option, Func<T, Option<TResult>> transform)
     => option.HasValue ? transform(option.Value) : None<TResult>();
    public static Option<TResult, TError> FlatMap<T, TError, TResult>(this Option<T, TError> option, Func<T, Option<TResult, TError>> transform)
     => option.HasValue ? transform(option.Value) : None<TResult, TError>(option.Error);

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

    private sealed record OptionImpl<T>(bool HasValue, T? Value) : Option<T>;

    private sealed record OptionImpl<T, TError>(bool HasValue, T? Value, TError? Error) : Option<T, TError>;
}

internal static class OptionalCollectionExtensions
{
    public static IEnumerable<T> WhereSome<T>(this IEnumerable<Option<T>> options)
     => options.Where(v => v.HasValue).Select(v => v.Value.NotNull());
    public static IEnumerable<T> WhereSome<T, TError>(this IEnumerable<Option<T, TError>> options)
     => options.Where(v => v.HasValue).Select(v => v.Value.NotNull());

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