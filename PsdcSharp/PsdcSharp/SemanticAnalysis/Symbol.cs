using Scover.Psdc.Parsing.Nodes;
using Scover.Psdc.Tokenization;
using static Scover.Psdc.Parsing.Nodes.Node;

namespace Scover.Psdc.StaticAnalysis;

internal interface CallableSymbol : Symbol
{
    public bool HasBeenDefined { get; }
    public IReadOnlyCollection<Parameter> Parameters { get; }
    public void MarkAsDefined();
}

internal interface Symbol
{
    public Identifier Name { get; }
    public Partition<Token> SourceTokens { get; }
    internal record Variable(Identifier Name, Partition<Token> SourceTokens,
        EvaluatedType Type)
    : Symbol
    {
        public virtual bool Equals(Variable? other) => other is not null
         && other.Name.Equals(Name)
         && other.Type.Equals(Type);
        public override int GetHashCode() => HashCode.Combine(Name, Type);
    }

    internal sealed record Constant(Identifier Name, Partition<Token> SourceTokens,
        EvaluatedType Type,
        Expression Value)
    : Variable(Name, SourceTokens, Type)
    {
        public bool Equals(Constant? other) => base.Equals(other)
         && other.Value.Equals(Value);
        public override int GetHashCode() => HashCode.Combine(Name, Type, Value);
    }

    internal sealed record TypeAlias(Identifier Name, Partition<Token> SourceTokens,
        EvaluatedType TargetType)
    : Symbol
    {
        public bool Equals(TypeAlias? other) => other is not null
         && other.Name.Equals(Name)
         && other.TargetType.Equals(TargetType);
        public override int GetHashCode() => HashCode.Combine(Name, TargetType);
    }

    internal sealed record Procedure(Identifier Name, Partition<Token> SourceTokens,
        IReadOnlyCollection<Parameter> Parameters)
    : CallableSymbol
    {
        public bool HasBeenDefined { get; private set; }
        public void MarkAsDefined() => HasBeenDefined = true;
        public bool Equals(Procedure? other) => other is not null
         && other.Name.Equals(Name)
         && other.Parameters.SequenceEqual(Parameters);
        public override int GetHashCode() => HashCode.Combine(Name, Parameters.GetSequenceHashCode());
    }

    internal sealed record Function(Identifier Name, Partition<Token> SourceTokens,
        IReadOnlyCollection<Parameter> Parameters,
        EvaluatedType ReturnType)
    : CallableSymbol
    {
        public bool HasBeenDefined { get; private set; }
        public void MarkAsDefined() => HasBeenDefined = true;
        public bool Equals(Function? other) => other is not null
         && other.Name.Equals(Name)
         && other.Parameters.SequenceEqual(Parameters)
         && other.ReturnType.Equals(ReturnType);
        public override int GetHashCode() => HashCode.Combine(Name, Parameters.GetSequenceHashCode(), ReturnType);

    }

    internal sealed record Parameter(Identifier Name, Partition<Token> SourceTokens,
        EvaluatedType Type,
        ParameterMode Mode)
    : Variable(Name, SourceTokens, Type)
    {
        public bool Equals(Parameter? other) => base.Equals(other)
         && other.Mode.Equals(Mode);
        public override int GetHashCode() => HashCode.Combine(Name, Type, Mode);
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
