using Scover.Psdc.Parsing;

namespace Scover.Psdc.Language;

interface Symbol : EquatableSemantics<Symbol>
{
    public Identifier Name { get; }
    public SourceTokens SourceTokens { get; }

    internal interface NameTypeBinding : Symbol
    {
        EvaluatedType Type { get; }
    }

    interface Callable : Symbol
    {
        public bool HasBeenDefined { get; }
        public IReadOnlyCollection<Parameter> Parameters { get; }

        public void MarkAsDefined();
    }

    internal sealed record Variable(Identifier Name, SourceTokens SourceTokens,
        EvaluatedType Type, Option<Value> Initializer)
    : NameTypeBinding
    {
        public bool SemanticsEqual(Symbol other) => other is Variable o
         && o.Name.SemanticsEqual(Name)
         && o.Type.SemanticsEqual(Type)
         && o.Initializer.OptionSemanticsEqual(Initializer);
    }

    internal sealed record Constant(Identifier Name, SourceTokens SourceTokens,
        EvaluatedType Type, Value Value)
    : NameTypeBinding
    {
        public bool SemanticsEqual(Symbol other) => other is Constant o
         && o.Name.SemanticsEqual(Name)
         && o.Type.SemanticsEqual(Type)
         && o.Value.SemanticsEqual(Value);
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
    : Callable
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
    : Callable
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
    : NameTypeBinding
    {
        public bool SemanticsEqual(Symbol other) => other is Parameter o
         && o.Name.SemanticsEqual(Name)
         && o.Type.SemanticsEqual(Type)
         && o.Mode == Mode;
    }
}

static class SymbolExtensions
{
    static readonly Dictionary<Type, string> symbolKinds = new() {
        [typeof(Symbol.Constant)] = "constant",
        [typeof(Symbol.Function)] = "function",
        [typeof(Symbol.Parameter)] = "parameter",
        [typeof(Symbol.Procedure)] = "procedure",
        [typeof(Symbol.TypeAlias)] = "type alias",
        [typeof(Symbol.Variable)] = "variable",
        [typeof(Symbol.NameTypeBinding)] = "variable",
    };

    public static string GetKind(this Symbol s) => symbolKinds[s.GetType()];

    public static string GetKind<T>() where T : Symbol => symbolKinds[typeof(T)];
}
