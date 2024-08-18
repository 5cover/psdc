namespace Scover.Psdc.Language;

interface ValueStatus : IEquatable<ValueStatus>
{
    ValueOption<object> ComptimeValue { get; }

    /// <summary>
    /// The value is known at compile-time.
    /// </summary>
    internal class Comptime : ValueStatus
    {
        /// <summary>
        /// Status for a value that is known at compile-time.
        /// </summary>
        /// <typeparam name="TUnderlying">The type of the underlying value.</typeparam>
        /// <param name="value">The actual value.</param>

        public static Comptime<TUnderlying> Of<TUnderlying>(TUnderlying value) where TUnderlying : notnull => new(value);
        protected Comptime(object value) => Value = value;
        public object Value { get; }
        ValueOption<object> ValueStatus.ComptimeValue => Value;
        public bool Equals(ValueStatus? other) => other is Comptime o && Equals(o.Value, Value);
    }
    internal sealed class Comptime<TUnderlying>(TUnderlying value) : Comptime(value), ValueStatus<TUnderlying>
    where TUnderlying : notnull
    {
        public new TUnderlying Value => value;
        public ValueOption<TUnderlying> ComptimeValue => value;
        ValueStatus<TResult> ValueStatus<TUnderlying>.Map<TResult>(Func<TUnderlying, TResult> transform) => Of(transform(value));
    }

    /// <summary>
    /// The value is known at run-time.
    /// </summary>
    /// <remarks>Example: return value of a function call.</remarks>
    internal abstract class Runtime : ValueStatus
    {
        ValueOption<object> ValueStatus.ComptimeValue => default;
        public bool Equals(ValueStatus? other) => other is Runtime;
        public static Runtime Instance { get; } = Runtime<object>.Instance;
    }
    internal sealed class Runtime<TUnderlying> : Runtime, ValueStatus<TUnderlying>
    {
        Runtime() { }
        public static new Runtime<TUnderlying> Instance { get; } = new();
        public ValueOption<TUnderlying> ComptimeValue => default;
        ValueStatus<TResult> ValueStatus<TUnderlying>.Map<TResult>(Func<TUnderlying, TResult> transform) => Runtime<TResult>.Instance;
    }

    /// <summary>
    /// The value is neither known at compile-time or run-time.
    /// </summary>
    /// <remarks>Example: unitialized variable, unallocated memory.</remarks>
    internal abstract class Garbage : ValueStatus
    {
        ValueOption<object> ValueStatus.ComptimeValue => default;
        public bool Equals(ValueStatus? other) => other is Garbage;
        public static Garbage Instance { get; } = Garbage<object>.Instance;
    }
    internal sealed class Garbage<TUnderlying> : Garbage, ValueStatus<TUnderlying>
    {
        Garbage() { }
        public static new Garbage<TUnderlying> Instance { get; } = new();
        public ValueOption<TUnderlying> ComptimeValue => default;
        ValueStatus<TResult> ValueStatus<TUnderlying>.Map<TResult>(Func<TUnderlying, TResult> transform) => Garbage<TResult>.Instance;
    }

    /// <summary>
    /// The value is a semantically invalid and causes a compilation error.
    /// </summary>
    internal abstract class Invalid : ValueStatus
    {
        ValueOption<object> ValueStatus.ComptimeValue => default;
        public bool Equals(ValueStatus? other) => other is Invalid;
        public static Invalid Instance { get; } = Invalid<object>.Instance;
    }
    internal sealed class Invalid<TUnderlying> : Invalid, ValueStatus<TUnderlying>
    {
        Invalid() { }
        public static new Invalid<TUnderlying> Instance { get; } = new();
        public ValueOption<TUnderlying> ComptimeValue => default;
        ValueStatus<TResult> ValueStatus<TUnderlying>.Map<TResult>(Func<TUnderlying, TResult> transform) => Invalid<TResult>.Instance;
    }
}

interface ValueStatus<TUnderlying> : ValueStatus
{
    public ValueStatus<TResult> Map<TResult>(Func<TUnderlying, TResult> transform) where TResult : notnull;
    public new ValueOption<TUnderlying> ComptimeValue { get; }
}
