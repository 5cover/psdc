using Scover.Psdc.Parsing;

namespace Scover.Psdc.Language;

public interface Symbol : EquatableSemantics<Symbol>
{
    private const string KindParameter = "parameter";
    private const string KindFunction = "function";
    private const string KindProcedure = "procedure";
    private const string KindTypeAlias = "type alias";
    private const string KindConstant = "constant";
    private const string KindLocalVariable = "local variable";
    private const string KindCallable = "callable";
    private const string KindVariable = "variable";
    private const string KindSymbol = "symbol";

    private static readonly Lazy<Dictionary<Type, string>> symbolKinds = new(() => new() {
        [typeof(Parameter)] = KindParameter,
        [typeof(Function)] = KindFunction,
        [typeof(Procedure)] = KindProcedure,
        [typeof(TypeAlias)] = KindTypeAlias,
        [typeof(Constant)] = KindConstant,
        [typeof(LocalVariable)] = KindLocalVariable,
        [typeof(Callable)] = KindCallable,
        [typeof(Variable)] = KindVariable,
        [typeof(Symbol)] = KindSymbol,
    });

    public static string GetKind<T>() where T : Symbol => symbolKinds.Value[typeof(T)];

    public string Kind { get; }

    public Identifier Name { get; }
    public SourceTokens SourceTokens { get; }

    internal abstract record Variable(Identifier Name, SourceTokens SourceTokens, EvaluatedType Type) : Symbol
    {
        public virtual string Kind => KindVariable;

        public abstract bool SemanticsEqual(Symbol other);
    }

    internal abstract record Callable(Identifier Name, SourceTokens SourceTokens, IReadOnlyCollection<Parameter> Parameters) : Symbol
    {
        public virtual string Kind => KindCallable;
        public bool HasBeenDefined { get; private set; }
        public void MarkAsDefined() => HasBeenDefined = true;
        public abstract bool SemanticsEqual(Symbol other);
    }

    internal sealed record LocalVariable(Identifier Name, SourceTokens SourceTokens,
        EvaluatedType Type, Option<Value> Initializer)
    : Variable(Name, SourceTokens, Type)
    {
        public override string Kind => KindLocalVariable;
        public override bool SemanticsEqual(Symbol other) => other is LocalVariable o
         && o.Name.SemanticsEqual(Name)
         && o.Type.SemanticsEqual(Type)
         && o.Initializer.Equals(Initializer);
    }

    internal sealed record Constant(Identifier Name, SourceTokens SourceTokens,
        EvaluatedType Type, Value Value)
    : Variable(Name, SourceTokens, Type)
    {
        public override string Kind => KindConstant;
        public override bool SemanticsEqual(Symbol other) => other is Constant o
         && o.Name.SemanticsEqual(Name)
         && o.Type.SemanticsEqual(Type)
         && o.Value.Equals(Value);
    }

    internal sealed record TypeAlias(Identifier Name, SourceTokens SourceTokens,
        EvaluatedType Type)
    : Symbol
    {
        public string Kind => KindTypeAlias;
        public bool SemanticsEqual(Symbol other) => other is TypeAlias o
         && o.Name.SemanticsEqual(Name)
         && o.Type.SemanticsEqual(Type);
    }

    internal sealed record Procedure(Identifier Name, SourceTokens SourceTokens,
        IReadOnlyCollection<Parameter> Parameters)
    : Callable(Name, SourceTokens, Parameters)
    {
        public override string Kind => KindProcedure;
        public override bool SemanticsEqual(Symbol other) => other is Procedure o
         && o.Name.SemanticsEqual(Name)
         && o.Parameters.AllSemanticsEqual(Parameters);
    }

    internal sealed record Function(Identifier Name, SourceTokens SourceTokens,
        IReadOnlyCollection<Parameter> Parameters,
        EvaluatedType ReturnType)
    : Callable(Name, SourceTokens, Parameters)
    {
        public override string Kind => KindFunction;
        public override bool SemanticsEqual(Symbol other) => other is Function o
         && o.Name.SemanticsEqual(Name)
         && o.Parameters.AllSemanticsEqual(Parameters)
         && o.ReturnType.SemanticsEqual(ReturnType);
    }

    internal sealed record Parameter(Identifier Name, SourceTokens SourceTokens,
        EvaluatedType Type,
        ParameterMode Mode)
    : Variable(Name, SourceTokens, Type)
    {
        public override string Kind => KindParameter;
        public override bool SemanticsEqual(Symbol other) => other is Parameter o
         && o.Name.SemanticsEqual(Name)
         && o.Type.SemanticsEqual(Type)
         && o.Mode == Mode;
    }
}
