using Scover.Psdc.Parsing;

namespace Scover.Psdc.Language;

public interface Symbol : EquatableSemantics<Symbol>
{
    public Identifier Name { get; }
    public SourceTokens SourceTokens { get; }
    public static virtual string TypeKind => "symbol";
    public string Kind { get; }

    internal interface ValueProvider : Symbol
    {
        EvaluatedType Type { get; }
    }

    internal interface Callable : Symbol
    {
        public bool HasBeenDefined { get; }
        public IReadOnlyCollection<Parameter> Parameters { get; }
        public void MarkAsDefined();
    }

    internal sealed record Variable(Identifier Name, SourceTokens SourceTokens,
        EvaluatedType Type, Option<Value> Initializer)
    : ValueProvider
    {
        public static string TypeKind => "variable";
        public string Kind => TypeKind;
        public bool SemanticsEqual(Symbol other) => other is Variable o
         && o.Name.SemanticsEqual(Name)
         && o.Type.SemanticsEqual(Type)
         && o.Initializer.Equals(Initializer);
    }

    internal sealed record Constant(Identifier Name, SourceTokens SourceTokens,
        EvaluatedType Type, Value Value)
    : ValueProvider
    {
        public static string TypeKind => "constant";
        public string Kind => TypeKind;
        public bool SemanticsEqual(Symbol other) => other is Constant o
         && o.Name.SemanticsEqual(Name)
         && o.Type.SemanticsEqual(Type)
         && o.Value.Equals(Value);
    }

    internal sealed record TypeAlias(Identifier Name, SourceTokens SourceTokens,
        EvaluatedType Type)
    : Symbol
    {
        public static string TypeKind => "type alias";
        public string Kind => TypeKind;
        public bool SemanticsEqual(Symbol other) => other is TypeAlias o
         && o.Name.SemanticsEqual(Name)
         && o.Type.SemanticsEqual(Type);
    }

    internal sealed record Procedure(Identifier Name, SourceTokens SourceTokens,
        IReadOnlyCollection<Parameter> Parameters)
    : Callable
    {
        public static string TypeKind => "procedure";
        public string Kind => TypeKind;
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
        public static string TypeKind => "function";
        public string Kind => TypeKind;
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
    : ValueProvider
    {
        public static string TypeKind => TypeKind;
        public string Kind => "parameter";
        public bool SemanticsEqual(Symbol other) => other is Parameter o
         && o.Name.SemanticsEqual(Name)
         && o.Type.SemanticsEqual(Type)
         && o.Mode == Mode;
    }
}
