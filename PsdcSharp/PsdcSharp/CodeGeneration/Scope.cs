using Scover.Psdc.CodeGeneration;

namespace Scover.Psdc;

internal interface Symbol
{
    internal sealed record Variable(TypeInfo Type) : Symbol;

    internal sealed record Constant(TypeInfo Type, string Value) : Symbol;
    internal sealed record TypeAlias(TypeInfo TargetType) : Symbol;
}

internal interface ReadOnlyScope
{
    Option<T> GetSymbol<T>(string name) where T : Symbol;
}

internal sealed class Scope : ReadOnlyScope
{
    private readonly Stack<Dictionary<string, Symbol>> _symbolTables = new();

    public IReadOnlyDictionary<string, Symbol> Symbols => _symbolTables.Peek();

    public Option<T> GetSymbol<T>(string name) where T : Symbol => _symbolTables
        .SelectMany(symbolTable => symbolTable)
        .FirstOrNone(kvp => kvp.Key == name)
        .FlatMap(kvp => kvp.Value is T t ? t.Some() : Option.None<T>());

    public void Pop() => _symbolTables.Pop();

    public void Push() => _symbolTables.Push(new());

    public bool TryAdd(string name, Symbol symbol) => _symbolTables.Peek().TryAdd(name, symbol);
}
