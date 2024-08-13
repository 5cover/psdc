namespace Scover.Psdc.Language;

enum ValueStatus
{
    /// <summary>
    /// The value is known at compile-time.
    /// </summary>
    Comptime,
    /// <summary>
    /// The value is known at run-time.
    /// </summary>
    /// <remarks>Example: return value of a function call.</remarks>
    Runtime,
    /// <summary>
    /// The value is neither known at compile-time or run-time.
    /// </summary>
    /// <remarks>Example: unitialized variable, unallocated memory.</remarks>
    Garbage,
    /// <summary>
    /// The value is a semantically invalid and causes a compilation error.
    /// </summary>
    Invalid,
}

interface ValueStatus<TUnderlying> : EquatableSemantics<ValueStatus<TUnderlying>>
{
    ValueStatus Status { get; }

    ValueStatus<TResult> Map<TResult>(Func<TUnderlying, TResult> transform);

    Option<TUnderlying> Comptime { get; }

    readonly record struct ComptimeValue(TUnderlying Value) : ValueStatus<TUnderlying>
    {
        public ValueStatus Status => ValueStatus.Comptime;

        public Option<TUnderlying> Comptime => Value.Some();

        public bool SemanticsEqual(ValueStatus<TUnderlying> other) => other is ComptimeValue o
         && EqualityComparer<TUnderlying>.Default.Equals(o.Value, Value);
        public ValueStatus<TResult> Map<TResult>(Func<TUnderlying, TResult> transform) => Language.Value.Comptime(transform(Value));
    }

    class RuntimeValue : ValueStatus<TUnderlying>
    {
        RuntimeValue() {}
        public static RuntimeValue Instance { get; } = new();
        public ValueStatus Status => ValueStatus.Runtime;

        public Option<TUnderlying> Comptime => Option.None<TUnderlying>();

        public bool SemanticsEqual(ValueStatus<TUnderlying> other) => other is RuntimeValue;
        public ValueStatus<TResult> Map<TResult>(Func<TUnderlying, TResult> transform) => Value.Runtime<TResult>();
    }

    class GarbageValue : ValueStatus<TUnderlying>
    {
        GarbageValue() {}
        public static GarbageValue Instance { get; } = new();
        public ValueStatus Status => ValueStatus.Garbage;

        public Option<TUnderlying> Comptime => Option.None<TUnderlying>();

        public bool SemanticsEqual(ValueStatus<TUnderlying> other) => other is GarbageValue;
        public ValueStatus<TResult> Map<TResult>(Func<TUnderlying, TResult> transform) => Value.Garbage<TResult>();
    }

    class InvalidValue : ValueStatus<TUnderlying>
    {
        InvalidValue() {}
        public static InvalidValue Instance { get; } = new();
        public ValueStatus Status => ValueStatus.Invalid;

        public Option<TUnderlying> Comptime => Option.None<TUnderlying>();

        public bool SemanticsEqual(ValueStatus<TUnderlying> other) => other is InvalidValue;
        
        public ValueStatus<TResult> Map<TResult>(Func<TUnderlying, TResult> transform) => Value.Invalid<TResult>();
    }
}
