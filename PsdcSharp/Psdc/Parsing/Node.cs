using System.Globalization;
using Scover.Psdc.Language;

namespace Scover.Psdc.Parsing;

interface CallNode : Node
{
    public Identifier Name { get; }
    public IReadOnlyCollection<ParameterActual> Parameters { get; }
}

interface ScopedNode : Node;

interface AliasReferenceNone : Node
{
    public Identifier Name { get; }
}

interface BlockNode : ScopedNode
{
    IReadOnlyCollection<Statement> Block { get; }
}

interface BracketedExpressionNode : Node.Expression
{
    public Expression ContainedExpression { get; }
}

public interface Node : EquatableSemantics<Node>
{
    SourceTokens SourceTokens { get; }

    public sealed record Algorithm(SourceTokens SourceTokens, Identifier Name, IReadOnlyCollection<Declaration> Declarations) : ScopedNode
    {
        public bool SemanticsEqual(Node other) => other is Algorithm o
         && o.Name.SemanticsEqual(Name)
         && o.Declarations.AllSemanticsEqual(Declarations);
    }

    public interface Declaration : Node
    {
        internal sealed record MainProgram(SourceTokens SourceTokens,
            IReadOnlyCollection<Statement> Block)
        : Declaration, BlockNode
        {
            public bool SemanticsEqual(Node other) => other is MainProgram o
             && o.Block.AllSemanticsEqual(Block);
        }

        internal sealed record TypeAlias(SourceTokens SourceTokens,
            Identifier Name,
            Type Type)
        : Declaration
        {
            public bool SemanticsEqual(Node other) => other is TypeAlias o
             && o.Name.SemanticsEqual(Name)
             && o.Type.SemanticsEqual(Type);
        }

        internal sealed record CompleteTypeAlias(SourceTokens SourceTokens,
            Identifier Name,
            Type.Complete Type)
        : Declaration
        {
            public bool SemanticsEqual(Node other) => other is CompleteTypeAlias o
             && o.Name.SemanticsEqual(Name)
             && o.Type.SemanticsEqual(Type);
        }

        internal sealed record Constant(SourceTokens SourceTokens,
            Type Type,
            Identifier Name,
            Expression Value)
        : Declaration
        {
            public bool SemanticsEqual(Node other) => other is Constant o
             && o.Name.SemanticsEqual(Name)
             && o.Type.SemanticsEqual(Type)
             && o.Value.SemanticsEqual(Value);
        }

        internal sealed record Procedure(SourceTokens SourceTokens,
            ProcedureSignature Signature)
        : Declaration
        {
            public bool SemanticsEqual(Node other) => other is Procedure o
             && o.Signature.SemanticsEqual(Signature);
        }

        internal sealed record ProcedureDefinition(SourceTokens SourceTokens,
            ProcedureSignature Signature,
            IReadOnlyCollection<Statement> Block)
        :  Declaration, BlockNode
        {
            public bool SemanticsEqual(Node other) => other is ProcedureDefinition o
             && o.Signature.SemanticsEqual(Signature)
             && o.Block.AllSemanticsEqual(Block);
        }

        internal sealed record Function(SourceTokens SourceTokens,
            FunctionSignature Signature)
        : Declaration
        {
            public bool SemanticsEqual(Node other) => other is Function o
             && o.Signature.SemanticsEqual(Signature);
        }

        internal sealed record FunctionDefinition(SourceTokens SourceTokens,
            FunctionSignature Signature,
            IReadOnlyCollection<Statement> Block)
        : Declaration, BlockNode
        {
            public bool SemanticsEqual(Node other) => other is FunctionDefinition o
             && o.Signature.SemanticsEqual(Signature)
             && o.Block.AllSemanticsEqual(Block);
        }
    }

    internal interface Statement : Node
    {
        internal sealed record Nop(SourceTokens SourceTokens)
        : Statement
        {
            public bool SemanticsEqual(Node other) => other is Nop;
        }

        internal sealed record Alternative(SourceTokens SourceTokens,
            Alternative.IfClause If,
            IReadOnlyCollection<Alternative.ElseIfClause> ElseIfs,
            Option<Alternative.ElseClause> Else)
        : Statement
        {

            public bool SemanticsEqual(Node other) => other is Alternative o
             && o.If.SemanticsEqual(If)
             && o.ElseIfs.AllSemanticsEqual(ElseIfs)
             && o.Else.OptionSemanticsEqual(Else);

            internal sealed record IfClause(SourceTokens SourceTokens,
                Expression Condition,
                IReadOnlyCollection<Statement> Block)
            : BlockNode
            {
                public bool SemanticsEqual(Node other) => other is IfClause o
                 && o.Condition.SemanticsEqual(Condition)
                 && o.Block.AllSemanticsEqual(Block);
            }

            internal sealed record ElseIfClause(SourceTokens SourceTokens,
                Expression Condition,
                IReadOnlyCollection<Statement> Block)
            : BlockNode
            {
                public bool SemanticsEqual(Node other) => other is ElseIfClause o
                 && o.Condition.SemanticsEqual(Condition)
                 && o.Block.AllSemanticsEqual(Block);
            }

            internal sealed record ElseClause(SourceTokens SourceTokens,
                IReadOnlyCollection<Statement> Block)
            : BlockNode
            {
                public bool SemanticsEqual(Node other) => other is ElseClause o
                 && o.Block.AllSemanticsEqual(Block);
            }
        }

        internal sealed record Switch(SourceTokens SourceTokens,
            Expression Expression,
            IReadOnlyCollection<Switch.Case> Cases,
            Option<Switch.DefaultCase> Default)
        : Statement
        {
            public bool SemanticsEqual(Node other) => other is Switch o
                 && o.Expression.SemanticsEqual(Expression)
                 && o.Cases.AllSemanticsEqual(Cases)
                 && o.Default.OptionSemanticsEqual(Default);

            internal sealed record Case(SourceTokens SourceTokens,
                Expression When,
                IReadOnlyCollection<Statement> Block)
            : BlockNode
            {
                public bool SemanticsEqual(Node other) => other is Case o
                 && o.When.SemanticsEqual(When)
                 && o.Block.AllSemanticsEqual(Block);
            }

            internal sealed record DefaultCase(SourceTokens SourceTokens,
                IReadOnlyCollection<Statement> Block)
            : BlockNode
            {
                public bool SemanticsEqual(Node other) => other is DefaultCase o
                 && o.Block.AllSemanticsEqual(Block);
            }
        }

        internal sealed record Assignment(SourceTokens SourceTokens,
            Expression.Lvalue Target,
            Expression Value)
        : Statement
        {
            public bool SemanticsEqual(Node other) => other is Assignment o
             && o.Target.SemanticsEqual(Target)
             && o.Value.SemanticsEqual(Value);
        }

        internal sealed record DoWhileLoop(SourceTokens SourceTokens,
            Expression Condition,
            IReadOnlyCollection<Statement> Block)
        : BlockNode, Statement
        {
            public bool SemanticsEqual(Node other) => other is DoWhileLoop o
             && o.Condition.SemanticsEqual(Condition)
                && o.Block.AllSemanticsEqual(Block);
        }

        internal interface Builtin : Statement
        {
            internal sealed record Ecrire(SourceTokens SourceTokens,
                Expression ArgumentNomLog,
                Expression ArgumentExpression)
            : Builtin
            {
                public bool SemanticsEqual(Node other) => other is Ecrire o
                 && o.ArgumentNomLog.SemanticsEqual(ArgumentNomLog)
                 && o.ArgumentExpression.SemanticsEqual(ArgumentExpression);
            }

            internal sealed record Fermer(SourceTokens SourceTokens,
                Expression ArgumentNomLog)
            : Builtin
            {
                public bool SemanticsEqual(Node other) => other is Fermer o
                 && o.ArgumentNomLog.SemanticsEqual(ArgumentNomLog);
            }

            internal sealed record Lire(SourceTokens SourceTokens,
                Expression ArgumentNomLog,
                Expression.Lvalue ArgumentVariable)
            : Builtin
            {
                public bool SemanticsEqual(Node other) => other is Lire o
                 && o.ArgumentNomLog.SemanticsEqual(ArgumentNomLog)
                 && o.ArgumentVariable.SemanticsEqual(ArgumentVariable);
            }

            internal sealed record OuvrirAjout(SourceTokens SourceTokens,
                Expression ArgumentNomLog)
            : Builtin
            {
                public bool SemanticsEqual(Node other) => other is OuvrirAjout o
                 && o.ArgumentNomLog.SemanticsEqual(ArgumentNomLog);
            }

            internal sealed record OuvrirEcriture(SourceTokens SourceTokens,
                Expression ArgumentNomLog)
            : Builtin
            {
                public bool SemanticsEqual(Node other) => other is OuvrirEcriture o
                 && o.ArgumentNomLog.SemanticsEqual(ArgumentNomLog);
            }

            internal sealed record OuvrirLecture(SourceTokens SourceTokens,
                Expression ArgumentNomLog)
            : Builtin
            {
                public bool SemanticsEqual(Node other) => other is OuvrirLecture o
                 && o.ArgumentNomLog.SemanticsEqual(ArgumentNomLog);
            }

            internal sealed record Assigner(SourceTokens SourceTokens,
                Expression.Lvalue ArgumentNomLog,
                Expression ArgumentNomExt)
            : Builtin
            {
                public bool SemanticsEqual(Node other) => other is Assigner o
                 && o.ArgumentNomLog.SemanticsEqual(ArgumentNomLog)
                 && o.ArgumentNomExt.SemanticsEqual(ArgumentNomExt);
            }

            internal sealed record EcrireEcran(SourceTokens SourceTokens,
                IReadOnlyCollection<Expression> Arguments)
            : Builtin
            {
                public bool SemanticsEqual(Node other) => other is EcrireEcran o
                 && o.Arguments.AllSemanticsEqual(Arguments);
            }

            internal sealed record LireClavier(SourceTokens SourceTokens,
                Expression.Lvalue ArgumentVariable)
            : Builtin
            {
                public bool SemanticsEqual(Node other) => other is LireClavier o
                 && o.ArgumentVariable.SemanticsEqual(ArgumentVariable);
            }
        }

        internal sealed record ForLoop(SourceTokens SourceTokens,
            Expression.Lvalue Variant,
            Expression Start,
            Expression End,
            Option<Expression> Step,
            IReadOnlyCollection<Statement> Block)
        : Statement, BlockNode
        {
            public bool SemanticsEqual(Node other) => other is ForLoop o
             && o.Variant.SemanticsEqual(Variant)
             && o.Start.SemanticsEqual(Start)
             && o.End.SemanticsEqual(End)
             && o.Step.OptionSemanticsEqual(Step)
             && o.Block.AllSemanticsEqual(Block);
        }

        internal sealed record ProcedureCall(SourceTokens SourceTokens,
            Identifier Name,
            IReadOnlyCollection<ParameterActual> Parameters)
        : Statement, CallNode
        {
            public bool SemanticsEqual(Node other) => other is ProcedureCall o
             && o.Name.SemanticsEqual(Name)
             && o.Parameters.AllSemanticsEqual(Parameters);
        }

        internal sealed record RepeatLoop(SourceTokens SourceTokens,
            Expression Condition,
            IReadOnlyCollection<Statement> Block)
        : BlockNode, Statement
        {
            public bool SemanticsEqual(Node other) => other is RepeatLoop o
             && o.Condition.SemanticsEqual(Condition)
             && o.Block.AllSemanticsEqual(Block);
        }

        internal sealed record Return(SourceTokens SourceTokens,
            Expression Value)
        : Statement
        {
            public bool SemanticsEqual(Node other) => other is Return o
             && o.Value.SemanticsEqual(Value);
        }

        internal sealed record LocalVariable(SourceTokens SourceTokens,
            NameTypeBinding Binding,
            Option<Initializer> Initializer)
        : Statement
        {
            public bool SemanticsEqual(Node other) => other is LocalVariable o
             && o.Binding.SemanticsEqual(Binding)
             && o.Initializer.OptionSemanticsEqual(Initializer);
        }

        internal sealed record WhileLoop(SourceTokens SourceTokens,
            Expression Condition,
            IReadOnlyCollection<Statement> Block)
        : BlockNode, Statement
        {
            public bool SemanticsEqual(Node other) => other is WhileLoop o
             && o.Condition.SemanticsEqual(Condition)
             && o.Block.AllSemanticsEqual(Block);
        }
    }

    internal interface Initializer : Node
    {
        internal sealed record Braced(SourceTokens SourceTokens,
            IReadOnlyList<Braced.Value> Values)
        : Initializer
        {
            public bool SemanticsEqual(Node other) => other is Braced o
             && o.Values.AllSemanticsEqual(Values);

            public sealed record Value(SourceTokens SourceTokens,
                Option<Designator> Designator,
                Initializer Initializer)
            : Node
            {
                public bool SemanticsEqual(Node other) => other is Value o
                 && o.Designator.OptionSemanticsEqual(Designator)
                 && o.Initializer.SemanticsEqual(Initializer);
            }
        }
    }

    internal interface Expression : Initializer
    {
        internal interface Lvalue : Expression
        {
            internal sealed record ComponentAccess(SourceTokens SourceTokens,
                Expression Structure,
                Identifier ComponentName)
            : Lvalue
            {
                public bool SemanticsEqual(Node other) => other is ComponentAccess o
                 && o.Structure.SemanticsEqual(Structure)
                 && o.ComponentName.SemanticsEqual(ComponentName);
            }

            internal sealed new record Bracketed
            : Lvalue, BracketedExpressionNode
            {
                public Bracketed(SourceTokens sourceTokens,
                Lvalue lvalue) => (SourceTokens, ContainedLvalue) = (sourceTokens,
                    lvalue is BracketedExpressionNode b
                    && b.ContainedExpression is Lvalue l
                    ? l : lvalue);
                public SourceTokens SourceTokens { get; }
                public Lvalue ContainedLvalue { get; }
                Expression BracketedExpressionNode.ContainedExpression => ContainedLvalue;

                public bool SemanticsEqual(Node other) => other is Bracketed o
                 && o.ContainedLvalue.SemanticsEqual(ContainedLvalue);
            }

            internal sealed record ArraySubscript(SourceTokens SourceTokens,
                Expression Array,
                IReadOnlyList<Expression> Index)
            : Lvalue
            {
                public bool SemanticsEqual(Node other) => other is ArraySubscript o
                 && o.Array.SemanticsEqual(Array)
                 && o.Index.AllSemanticsEqual(Index);
            }

            internal sealed record VariableReference(SourceTokens SourceTokens,
                Identifier Name)
            : Lvalue
            {
                public bool SemanticsEqual(Node other) => other is VariableReference o
                 && o.Name.SemanticsEqual(Name);
            }
        }

        internal sealed record UnaryOperation(SourceTokens SourceTokens,
            UnaryOperator Operator,
            Expression Operand)
        : Expression
        {
            public bool SemanticsEqual(Node other) => other is UnaryOperation o
             && o.Operator == Operator
             && o.Operand.SemanticsEqual(Operand);
        }

        internal sealed record BinaryOperation(SourceTokens SourceTokens,
            Expression Left,
            BinaryOperator Operator,
            Expression Right)
        : Expression
        {
            public bool SemanticsEqual(Node other) => other is BinaryOperation o
             && o.Left.SemanticsEqual(Left)
             && o.Operator == Operator
             && o.Right.SemanticsEqual(Right);
        }

        internal sealed record BuiltinFdf(SourceTokens SourceTokens,
            Expression ArgumentNomLog)
        : Expression
        {
            public bool SemanticsEqual(Node other) => other is BuiltinFdf o
             && o.ArgumentNomLog.SemanticsEqual(ArgumentNomLog);
        }

        internal sealed record FunctionCall(SourceTokens SourceTokens,
            Identifier Name,
            IReadOnlyCollection<ParameterActual> Parameters)
        : Expression, CallNode
        {
            public bool SemanticsEqual(Node other) => other is FunctionCall o
             && o.Name.SemanticsEqual(Name)
             && o.Parameters.AllSemanticsEqual(Parameters);
        }

        internal sealed record Bracketed
        : BracketedExpressionNode
        {
            public Bracketed(SourceTokens sourceTokens,
            Expression expr) => (SourceTokens, ContainedExpression) = (sourceTokens,
                expr is BracketedExpressionNode b ? b.ContainedExpression : expr);
            public SourceTokens SourceTokens { get; }
            public Expression ContainedExpression { get; }

            public bool SemanticsEqual(Node other) => other is Bracketed o
             && o.ContainedExpression.SemanticsEqual(ContainedExpression);
        }

        internal abstract record Literal<TType, TValue, TUnderlying>(SourceTokens SourceTokens,
            TType ValueType,
            TUnderlying Value)
        : Literal
            where TUnderlying : IConvertible
            where TValue : Value
            where TType : EvaluatedType, InstantiableType<TValue, TUnderlying>
        {
            IConvertible Literal.Value => Value;
            EvaluatedType Literal.ValueType => ValueType;

            public Value CreateValue() => ValueType.Instantiate(Value);

            public bool SemanticsEqual(Node other) => other is Literal<TType, TValue, TUnderlying> o
             && o.Value.Equals(Value);
        }

        internal interface Literal : Expression
        {
            IConvertible Value { get; }
            EvaluatedType ValueType { get; }
            Value CreateValue();

            internal sealed record True(SourceTokens SourceTokens)
            : Literal<BooleanType, BooleanValue, bool>(SourceTokens, BooleanType.Instance, true)
            {
            }

            internal sealed record False(SourceTokens SourceTokens)
            : Literal<BooleanType, BooleanValue, bool>(SourceTokens, BooleanType.Instance, false)
            {
            }

            internal sealed record Character(SourceTokens SourceTokens, char Value)
            : Literal<CharacterType, CharacterValue, char>(SourceTokens, CharacterType.Instance, Value)
            {
                public Character(SourceTokens SourceTokens, string valueStr)
                : this(SourceTokens, char.Parse(valueStr)) { }
            }

            internal sealed record Integer(SourceTokens SourceTokens, int Value)
            : Literal<IntegerType, IntegerValue, int>(SourceTokens, IntegerType.Instance, Value)
            {
                public Integer(SourceTokens sourceTokens, string valueStr)
                : this(sourceTokens, int.Parse(valueStr, CultureInfo.InvariantCulture)) { }
            }

            internal sealed record Real(SourceTokens SourceTokens, decimal Value)
            : Literal<RealType, RealValue, decimal>(SourceTokens, RealType.Instance, Value)
            {
                public Real(SourceTokens sourceTokens, string valueStr)
                : this(sourceTokens, decimal.Parse(valueStr, CultureInfo.InvariantCulture)) { }
            };

            internal sealed record String(SourceTokens SourceTokens, string Value)
            : Literal<LengthedStringType, LengthedStringValue, string>(SourceTokens, LengthedStringType.Create(Value.Length), Value);
        }
    }

    internal interface Type : Node
    {
        internal sealed record AliasReference(SourceTokens SourceTokens,
            Identifier Name)
        : Type, AliasReferenceNone
        {
            public bool SemanticsEqual(Node other) => other is AliasReference o
             && o.Name.SemanticsEqual(Name);
        }

        internal sealed record String(SourceTokens SourceTokens)
        : Type
        {
            public bool SemanticsEqual(Node other) => other is String;
        }

        internal interface Complete : Type
        {
            internal sealed record Array(SourceTokens SourceTokens,
                Type Type,
                IReadOnlyCollection<Expression> Dimensions)
            : Complete
            {
                public bool SemanticsEqual(Node other) => other is Array o
                 && o.Type.SemanticsEqual(Type)
                 && o.Dimensions.AllSemanticsEqual(Dimensions);
            }

            internal sealed record File(SourceTokens SourceTokens)
            : Complete
            {
                public bool SemanticsEqual(Node other) => other is File;
            }

            internal sealed record Character(SourceTokens SourceTokens)
             : Complete
            {
                public bool SemanticsEqual(Node other) => other is Character;
            }

            internal sealed record Boolean(SourceTokens SourceTokens)
             : Complete
            {
                public bool SemanticsEqual(Node other) => other is Boolean;
            }

            internal sealed record Integer(SourceTokens SourceTokens)
             : Complete
            {
                public bool SemanticsEqual(Node other) => other is Integer;
            }

            internal sealed record Real(SourceTokens SourceTokens)
             : Complete
            {
                public bool SemanticsEqual(Node other) => other is Real;
            }

            internal sealed new record AliasReference(SourceTokens SourceTokens,
                Identifier name)
            : Complete, AliasReferenceNone
            {
                public Identifier Name => name;

                public bool SemanticsEqual(Node other) => other is AliasReference o
                 && o.Name.SemanticsEqual(Name);
            }

            internal sealed record LengthedString(SourceTokens SourceTokens,
                Expression length)
            : Complete
            {
                public Expression Length => length;

                public bool SemanticsEqual(Node other) => other is LengthedString o
                 && o.Length.SemanticsEqual(Length);
            }

            internal sealed record Structure(SourceTokens SourceTokens,
                IReadOnlyList<NameTypeBinding> components)
            : Complete
            {
                public IReadOnlyList<NameTypeBinding> Components => components;

                public bool SemanticsEqual(Node other) => other is Structure o
                 && o.Components.AllSemanticsEqual(Components);
            }
        }
    }

    internal interface Designator : Node
    {
        internal sealed record Array(SourceTokens SourceTokens,
            IReadOnlyList<Expression> Index)
        : Designator
        {
            public bool SemanticsEqual(Node other) => other is Array o
             && o.Index.AllSemanticsEqual(Index);
        }

        internal sealed record Structure(SourceTokens SourceTokens,
            Identifier Component)
        : Designator
        {
            public bool SemanticsEqual(Node other) => other is Structure o
             && o.Component.SemanticsEqual(Component);
        }
    }

    internal sealed record NameTypeBinding(SourceTokens SourceTokens,
        IReadOnlyCollection<Identifier> Names,
        Type.Complete Type)
    : Node
    {
        public bool SemanticsEqual(Node other) => other is NameTypeBinding o
         && o.Names.AllSemanticsEqual(Names)
         && o.Type.SemanticsEqual(Type);
    }

    internal sealed record ParameterActual(SourceTokens SourceTokens,
        ParameterMode Mode,
        Expression Value)
    : Node
    {
        public bool SemanticsEqual(Node other) => other is ParameterActual o
         && o.Mode == Mode
         && o.Value.SemanticsEqual(Value);
    }

    internal sealed record ParameterFormal(SourceTokens SourceTokens,
        ParameterMode Mode,
        Identifier Name,
        Type Type)
    : Node
    {
        public bool SemanticsEqual(Node other) => other is ParameterFormal o
         && o.Mode == Mode
         && o.Name.SemanticsEqual(Name)
         && o.Type.SemanticsEqual(Type);
    }

    internal sealed record ProcedureSignature(SourceTokens SourceTokens,
        Identifier Name,
        IReadOnlyCollection<ParameterFormal> Parameters)
    : Node
    {
        public bool SemanticsEqual(Node other) => other is ProcedureSignature o
         && o.Name.SemanticsEqual(Name)
         && o.Parameters.AllSemanticsEqual(Parameters);
    }

    internal sealed record FunctionSignature(SourceTokens SourceTokens,
        Identifier Name,
        IReadOnlyCollection<ParameterFormal> Parameters,
        Type ReturnType)
    : Node
    {
        public bool SemanticsEqual(Node other) => other is FunctionSignature o
         && o.Name.SemanticsEqual(Name)
         && o.Parameters.AllSemanticsEqual(Parameters)
         && o.ReturnType.SemanticsEqual(ReturnType);
    }
}

#region Terminals

sealed record ParameterMode
{
    ParameterMode(string formal, string actual)
     => (RepresentationFormal, RepresentationActual) = (formal, actual);

    public string RepresentationFormal { get; }
    public string RepresentationActual { get; }

    public static ParameterMode In { get; } = new("entF", "entE");
    public static ParameterMode Out { get; } = new("sortF", "sortE");
    public static ParameterMode InOut { get; } = new("entF/sortF", "entE/sortE");
}

public sealed class Identifier : Node, IEquatable<Identifier?>
{
    public Identifier(SourceTokens sourceTokens, string name)
    {
        (SourceTokens, Name) = (sourceTokens, name);
#if DEBUG
        if (!IsValidIdentifier(name)) {
            throw new ArgumentException($"`{name}` is not a valid identifier", nameof(name));
        }
        static bool IsValidIdentifier(string name) => name.Length > 0
         && (name[0] == '_' || char.IsLetter(name[0]))
         && name.Skip(1).All(c => c == '_' || char.IsLetterOrDigit(c));
#endif
    }

    public SourceTokens SourceTokens { get; }
    public string Name { get; }

    public bool SemanticsEqual(Node other) => other is Identifier o
     && o.Name == Name;

    // Equals and GetHashCode implementation for usage in dictionaries.
    public override bool Equals(object? obj) => Equals(obj as Identifier);

    public bool Equals(Identifier? other) => other is not null && other.Name == Name;

    public override int GetHashCode() => Name.GetHashCode();

    public override string ToString() => Name;
}

#endregion Terminals
