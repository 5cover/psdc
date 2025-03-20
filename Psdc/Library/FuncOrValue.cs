namespace Scover.Psdc.Library;

public readonly struct FuncOrValue<TArg, TResult>
{
    public FuncOrValue(TResult result) => _result = result;
    public FuncOrValue(Func<TArg, TResult> func) => _func = func;
    readonly TResult? _result;
    readonly Func<TArg, TResult>? _func;
    public TResult Get(TArg argument) => _result ?? _func.NotNull()(argument);

    public static implicit operator FuncOrValue<TArg, TResult>(TResult result) => new(result);
    public static implicit operator FuncOrValue<TArg, TResult>(Func<TArg, TResult> result) => new(result);
}
