using System.Diagnostics.CodeAnalysis;

namespace Scover.Psdc.Library;

// Covariant Option monad

public interface Option<out T>
{
    [MemberNotNullWhen(true, nameof(Value))]
    bool HasValue { get; }

    T? Value { get; }

    /// <summary>Returns an <see cref="ValueOption{T}"/> as an <see cref="Option{T}"/></summary>
    /// <remarks>Used to circumvent implicit casting limitations.</remarks>
    public static Option<T> Of(ValueOption<T> option) => option;
}

public interface Option<out T, out TError>
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

public readonly record struct ValueOption<T>(bool HasValue, T? Value) : Option<T>
{
    public static implicit operator ValueOption<T>(T value) => new(true, value);
}

public readonly record struct ValueOption<T, TError>(bool HasValue, T? Value, TError? Error) : Option<T, TError>
{
    public static implicit operator ValueOption<T, TError>(T value) => new(true, value, default);
    public static implicit operator ValueOption<T, TError>(TError error) => new(false, default, error);
}

public static class Option
{
    public static T Unwrap<T, TError>(this Option<T, TError> option)
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

    public static Option<TResult> Cast<T, TResult>(this Option<T> option)
     => option.HasValue && option.Value is TResult result ? result.Some() : None<TResult>();

    public static Option<TResult> Bind<T, TResult>(this Option<T> option, Func<T, Option<TResult>> transform)
     => option.HasValue ? transform(option.Value) : None<TResult>();

    public static Option<TResult, TError> Bind<T, TError, TResult>(this Option<T, TError> option,
        Func<T, Option<TResult, TError>> transform)
     => option.HasValue ? transform(option.Value) : None<TResult, TError>(option.Error);

    public static Option<TResult, TError> Bind<T1, T2, TError, TResult>(this Option<(T1, T2), TError> option,
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

    public static void MatchSome<T1, T2>(this Option<(T1, T2)> option, Action<T1, T2> action)
    {
        if (option.HasValue) {
            action(option.Value.Item1, option.Value.Item2);
        }
    }

    /// <summary>
    /// Applies a filter to this option.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="option">The option.</param>
    /// <param name="predicate">The predicate to match the option's value against</param>
    /// <returns>A Some option that matched the predicate or a None option.</returns>
    public static Option<T> When<T>(this Option<T> option, Func<T, bool> predicate)
     => option.HasValue && predicate(option.Value) ? option : None<T>();

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
