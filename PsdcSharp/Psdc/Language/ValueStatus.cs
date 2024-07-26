namespace Scover.Psdc.Language;

enum ValueStatus
{
    Comptime,
    Runtime,
    Uninitialized,
}

interface ValueStatus<TUnderlying> : EquatableSemantics<ValueStatus<TUnderlying>>
{
    ValueStatus Value { get; }

    ValueStatus<TResult> Map<TResult>(Func<TUnderlying, TResult> transform);

    Option<TUnderlying> Comptime { get; }

    readonly record struct ComptimeValue(TUnderlying Value) : ValueStatus<TUnderlying>
    {
        ValueStatus ValueStatus<TUnderlying>.Value => ValueStatus.Comptime;

        public Option<TUnderlying> Comptime => Value.Some();

        public bool SemanticsEqual(ValueStatus<TUnderlying> other) => other is ComptimeValue o
         && EqualityComparer<TUnderlying>.Default.Equals(o.Value, Value);

        public ValueStatus<TResult> Map<TResult>(Func<TUnderlying, TResult> transform) => Language.Value.Comptime(transform(Value));
    }

    class RuntimeValue : ValueStatus<TUnderlying>
    {
        ValueStatus ValueStatus<TUnderlying>.Value => ValueStatus.Runtime;

        public Option<TUnderlying> Comptime => Option.None<TUnderlying>();

        public bool SemanticsEqual(ValueStatus<TUnderlying> other) => other is RuntimeValue;
        public ValueStatus<TResult> Map<TResult>(Func<TUnderlying, TResult> transform) => Language.Value.Runtime<TResult>();
    }

    class UninitializedValue : ValueStatus<TUnderlying>
    {
        ValueStatus ValueStatus<TUnderlying>.Value => ValueStatus.Uninitialized;

        public Option<TUnderlying> Comptime => Option.None<TUnderlying>();

        public bool SemanticsEqual(ValueStatus<TUnderlying> other) => other is UninitializedValue;
        public ValueStatus<TResult> Map<TResult>(Func<TUnderlying, TResult> transform) => Language.Value.Uninitialized<TResult>();
    }
}
