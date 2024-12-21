using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Scover.Psdc.Library;

// Covariant Option monad

[SuppressMessage("Naming", "CA1716", Justification = "Might use the .NET option in a future version")]
public interface Option<out T>
{
    [MemberNotNullWhen(true, nameof(Value))]
    bool HasValue { get; }

    T? Value { get; }
}

[SuppressMessage("Naming", "CA1716", Justification = "Might use the .NET option in a future version")]
public interface Option<out T, out TError>
{
    TError? Error { get; }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    bool HasValue { get; }

    T? Value { get; }
}

public readonly record struct ValueOption<T> : Option<T>
{
    public ValueOption(bool hasValue, T? value)
    {
        HasValue = hasValue;
        Value = value;
    }

    [MemberNotNullWhen(true, nameof(Value))]
    public bool HasValue { get; }
    public T? Value { get; }

    public static implicit operator ValueOption<T>(T value) => new(true, value);
}

public readonly record struct ValueOption<T, TError> : Option<T, TError>
{
    public ValueOption() => Debug.Fail("Can't construct without parameters");
    public ValueOption(bool hasValue, T? value, TError? error)
    {
        HasValue = hasValue;
        Value = value;
        Error = error;
    }

    [MemberNotNullWhen(true, nameof(HasValue))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool HasValue { get; }
    public T? Value { get; }
    public TError? Error { get; }

    public static implicit operator ValueOption<T, TError>(T value) => new(true, value, default);
    public static implicit operator ValueOption<T, TError>(TError error) => new(false, default, error);
}

[SuppressMessage("Naming", "CA1716", Justification = "Might use the .NET option in a future version")]
public static class Option
{
    public static ValueOption<T> Some<T>(this T value) => new(true, value);
    public static ValueOption<T, TError> Some<T, TError>(this T value)
     => new(true, value, default);

    public static ValueOption<T> SomeAs<T>(this object obj)
     => obj is T t ? t.Some() : None<T>();

    public static ValueOption<T> None<T>() => new(false, default);
    public static ValueOption<T, TError> None<T, TError>(this TError error) => new(false, default, error);

    public static T Unwrap<T, TError>(this Option<T, TError> option)
     => option.HasValue ? option.Value : throw new InvalidOperationException("Option has no value");
    public static T Unwrap<T>(this Option<T> option)
     => option.HasValue ? option.Value : throw new InvalidOperationException("Option has no value");

    public static Option<(T1, T2)> Zip<T1, T2>(this Option<T1> option1, Option<T2> option2)
     => (option1.HasValue && option2.HasValue)
        ? (option1.Value, option2.Value).Some()
        : None<(T1, T2)>();
    public static Option<(T1, T2), TError> Zip<T1, T2, TError>(this Option<T1, TError> option1, Option<T2, TError> option2)
     => (option1.HasValue && option2.HasValue)
        ? Some<(T1, T2), TError>((option1.Value, option2.Value))
        : None<(T1, T2), TError>(option1.HasValue ? option2.Error.NotNull() : option1.Error);

    public static Option<TResult> Bind<T, TResult>(this Option<T> option, Func<T, Option<TResult>> transform)
     => option.HasValue ? transform(option.Value) : None<TResult>();
    public static ValueOption<TResult> Bind<T, TResult>(this Option<T> option, Func<T, ValueOption<TResult>> transform)
     => option.HasValue ? transform(option.Value) : None<TResult>();

    public static Option<TResult> Bind<T1, T2, TResult>(this Option<(T1, T2)> option, Func<T1, T2, Option<TResult>> transform)
     => option.HasValue ? transform(option.Value.Item1, option.Value.Item2) : None<TResult>();
    public static ValueOption<TResult> Bind<T1, T2, TResult>(this Option<(T1, T2)> option, Func<T1, T2, ValueOption<TResult>> transform)
     => option.HasValue ? transform(option.Value.Item1, option.Value.Item2) : None<TResult>();

    public static Option<TResult, TError> Bind<T, TResult, TError>(this Option<T, TError> option,
        Func<T, Option<TResult, TError>> transform)
     => option.HasValue ? transform(option.Value) : None<TResult, TError>(option.Error);
    public static ValueOption<TResult, TError> Bind<T, TResult, TError>(this Option<T, TError> option,
        Func<T, ValueOption<TResult, TError>> transform)
     => option.HasValue ? transform(option.Value) : None<TResult, TError>(option.Error);

    public static Option<TResult, TError> Bind<T1, T2, TResult, TError>(this Option<(T1, T2), TError> option,
        Func<T1, T2, Option<TResult, TError>> transform)
     => option.HasValue ? transform(option.Value.Item1, option.Value.Item2) : None<TResult, TError>(option.Error);
    public static ValueOption<TResult, TError> Bind<T1, T2, TResult, TError>(this Option<(T1, T2), TError> option,
        Func<T1, T2, ValueOption<TResult, TError>> transform)
     => option.HasValue ? transform(option.Value.Item1, option.Value.Item2) : None<TResult, TError>(option.Error);

    public static ValueOption<TResult> Map<T, TResult>(this Option<T> option, Func<T, TResult> transform)
     => option.HasValue ? transform(option.Value).Some() : default;
    public static ValueOption<TResult> Map<T1, T2, TResult>(this Option<(T1, T2)> option, Func<T1, T2, TResult> transform)
     => option.HasValue ? transform(option.Value.Item1, option.Value.Item2).Some() : default;
    public static ValueOption<TResult> Map<T1, T2, T3, TResult>(this Option<(T1, T2, T3)> option, Func<T1, T2, T3, TResult> transform)
     => option.HasValue ? transform(option.Value.Item1, option.Value.Item2, option.Value.Item3).Some() : default;
    public static ValueOption<TResult, TError> Map<T, TResult, TError>(this Option<T, TError> option, Func<T, TResult> transform)
     => option.HasValue ? transform(option.Value) : option.Error;
    public static ValueOption<TResult, TError> Map<T1, T2, TResult, TError>(this Option<(T1, T2), TError> option, Func<T1, T2, TResult> transform)
     => option.HasValue ? transform(option.Value.Item1, option.Value.Item2) : option.Error;
    public static ValueOption<TResult, TError> Map<T1, T2, T3, TResult, TError>(this Option<(T1, T2, T3), TError> option, Func<T1, T2, T3, TResult> transform)
     => option.HasValue ? transform(option.Value.Item1, option.Value.Item2, option.Value.Item3) : option.Error;

    public static ValueOption<T, TNewError> MapError<T, TError, TNewError>(this Option<T, TError> option, Func<TError, TNewError> transform)
     => option.HasValue ? Some<T, TNewError>(option.Value) : None<T, TNewError>(transform(option.Error));

    public static Option<T> Tap<T>(this Option<T> option, Action<T>? some = null, Action? none = null)
    {
        if (option.HasValue) {
            some?.Invoke(option.Value);
        } else {
            none?.Invoke();
        }
        return option;
    }
    public static Option<(T1, T2)> Tap<T1, T2>(this Option<(T1, T2)> option, Action<T1, T2>? some = null, Action? none = null)
    {
        if (option.HasValue) {
            some?.Invoke(option.Value.Item1, option.Value.Item2);
        } else {
            none?.Invoke();
        }
        return option;
    }
    public static Option<T, TError> Tap<T, TError>(this Option<T, TError> option, Action<T>? some = null, Action<TError>? none = null)
    {
        if (option.HasValue) {
            some?.Invoke(option.Value);
        } else {
            none?.Invoke(option.Error);
        }
        return option;
    }

    public static Option<(T1, T2), TError> Tap<T1, T2, TError>(this Option<(T1, T2), TError> option, Action<T1, T2>? some = null, Action<TError>? none = null)
    {
        if (option.HasValue) {
            some?.Invoke(option.Value.Item1, option.Value.Item2);
        } else {
            none?.Invoke(option.Error);
        }
        return option;
    }

    public static TResult Match<T, TResult>(this Option<T> option, Func<T, TResult> some, Func<TResult> none)
     => option.HasValue ? some(option.Value) : none();
    public static TResult Match<T1, T2, TResult>(this Option<(T1, T2)> option, Func<T1, T2, TResult> some, Func<TResult> none)
     => option.HasValue ? some(option.Value.Item1, option.Value.Item2) : none();
    public static TResult Match<T, TResult, TError>(this Option<T, TError> option, Func<T, TResult> some, Func<TError, TResult> none)
     => option.HasValue ? some(option.Value) : none(option.Error);
    public static TResult Match<T1, T2, TResult, TError>(this Option<(T1, T2), TError> option, Func<T1, T2, TResult> some, Func<TError, TResult> none)
     => option.HasValue ? some(option.Value.Item1, option.Value.Item2) : none(option.Error);

    public static Option<T> Must<T>(this Option<T> option, Predicate<T> predicate)
     => option.HasValue && predicate(option.Value) ? option : None<T>();
    public static Option<T, TError> Must<T, TError>(this Option<T, TError> option, Predicate<T> predicate, Func<T, TError> falseError)
     => option.HasValue
        ? predicate(option.Value)
            ? option
            : None<T, TError>(falseError(option.Value))
        : option;

    public static Option<T> Or<T>(this Option<T> option, Option<T> fallback)
     => option.HasValue ? option : fallback;
    public static ValueOption<T> Or<T>(this ValueOption<T> option, ValueOption<T> fallback)
        => option.HasValue ? option : fallback;
    public static Option<T> Or<T>(this Option<T> option, Func<Option<T>> fallback)
     => option.HasValue ? option : fallback();
    public static ValueOption<T> Or<T>(this ValueOption<T> option, Func<ValueOption<T>> fallback)
     => option.HasValue ? option : fallback();

    public static Option<T, TError> Or<T, TError>(this Option<T, TError> option, Option<T, TError> fallback)
     => option.HasValue ? option : fallback;
    public static ValueOption<T, TError> Or<T, TError>(this ValueOption<T, TError> option, ValueOption<T, TError> fallback)
     => option.HasValue ? option : fallback;
    public static Option<T, TError> Or<T, TError>(this Option<T, TError> option, Func<Option<T, TError>> fallback)
     => option.HasValue ? option : fallback();
    public static ValueOption<T, TError> Or<T, TError>(this ValueOption<T, TError> option, Func<ValueOption<T, TError>> fallback)
     => option.HasValue ? option : fallback();

    public static T ValueOr<T>(this Option<T> option, T defaultValue)
     => option.HasValue ? option.Value : defaultValue;
    public static T ValueOr<T, TError>(this Option<T, TError> option, T defaultValue)
     => option.HasValue ? option.Value : defaultValue;

    public static T ValueOr<T>(this Option<T> option, Func<T> defaultValue)
         => option.HasValue ? option.Value : defaultValue();
    public static T ValueOr<T, TError>(this Option<T, TError> option, Func<T> defaultValue)
     => option.HasValue ? option.Value : defaultValue();

    public static T? ValueOrDefault<T>(this Option<T> option)
     => option.HasValue ? option.Value : default;
    public static T? ValueOrDefault<T, TError>(this Option<T, TError> option)
     => option.HasValue ? option.Value : default;

    public static ValueOption<T, TError> OrWithError<T, TError>(this Option<T> option, TError error)
     => option.HasValue ? option.Value : error;

    public static ValueOption<T> DropError<T, TError>(this Option<T, TError> option, Action<TError>? actionWithError = null)
    {
        if (!option.HasValue) {
            actionWithError?.Invoke(option.Error);
        }
        return new(option.HasValue, option.Value);
    }
}
