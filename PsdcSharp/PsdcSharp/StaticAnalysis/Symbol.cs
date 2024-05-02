
using Scover.Psdc.Parsing;
using static Scover.Psdc.Parsing.Node;

namespace Scover.Psdc.StaticAnalysis;

internal interface CallableSymbol : Symbol
{
    public bool HasBeenDefined { get; }
    public IReadOnlyCollection<Parameter> Parameters { get; }
    public void MarkAsDefined();
}

internal interface Symbol : EquatableSemantics<Symbol>
{
    public Identifier Name { get; }
    public SourceTokens SourceTokens { get; }

    internal record Variable(Identifier Name, SourceTokens SourceTokens,
        EvaluatedType Type)
    : Symbol
    {
        public virtual bool SemanticsEqual(Symbol other) => other is Variable o
         && o.Name.SemanticsEqual(Name)
         && o.Type.SemanticsEqual(Type);
    }

    internal sealed record Constant(Identifier Name, SourceTokens SourceTokens,
        EvaluatedType Type,
        Expression Value)
    : Variable(Name, SourceTokens, Type)
    {
        public override bool SemanticsEqual(Symbol other) => other is Constant o
         && o.Value.SemanticsEqual(Value)
         && base.SemanticsEqual(other);
    }

    internal sealed record TypeAlias(Identifier Name, SourceTokens SourceTokens,
        EvaluatedType TargetType)
    : Symbol
    {
        public bool SemanticsEqual(Symbol other) => other is TypeAlias o
         && o.Name.SemanticsEqual(Name)
         && o.TargetType.SemanticsEqual(TargetType);
    }

    internal sealed record Procedure(Identifier Name, SourceTokens SourceTokens,
        IReadOnlyCollection<Parameter> Parameters)
    : CallableSymbol
    {
        public bool HasBeenDefined { get; private set; }
        public void MarkAsDefined() => HasBeenDefined = true;
        public bool SemanticsEqual(Symbol other) => other is Procedure o
         && o.Name.SemanticsEqual(Name)
         && o.Parameters.AllSemanticsEqual(Parameters);
    }

    internal sealed record Function(Identifier Name, SourceTokens SourceTokens,
        IReadOnlyCollection<Parameter> Parameters,
        EvaluatedType ReturnType)
    : CallableSymbol
    {
        public bool HasBeenDefined { get; private set; }
        public void MarkAsDefined() => HasBeenDefined = true;
        public bool SemanticsEqual(Symbol other) => other is Function o
         && o.Name.SemanticsEqual(Name)
         && o.Parameters.AllSemanticsEqual(Parameters)
         && o.ReturnType.SemanticsEqual(ReturnType);

    }

    internal sealed record Parameter(Identifier Name, SourceTokens SourceTokens,
        EvaluatedType Type,
        ParameterMode Mode)
    : Variable(Name, SourceTokens, Type)
    {
        public override bool SemanticsEqual(Symbol other) => other is Parameter o
         && o.Mode == Mode;
    }
}

internal static class SymbolExtensions
{
    private static readonly Dictionary<System.Type, string> symbolKinds = new() {
        [typeof(Symbol.Constant)] = "constant",
        [typeof(Symbol.Function)] = "function",
        [typeof(Symbol.Parameter)] = "parameter",
        [typeof(Symbol.Procedure)] = "procedure",
        [typeof(Symbol.TypeAlias)] = "type alias",
        [typeof(Symbol.Variable)] = "variable",
    };

    public static string GetKind(this Symbol s) => symbolKinds[s.GetType()];
    public static string GetKind<T>() where T : Symbol => symbolKinds[typeof(T)];
}
