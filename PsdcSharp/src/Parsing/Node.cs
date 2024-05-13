using System.Globalization;

using Scover.Psdc.Language;

namespace Scover.Psdc.Parsing;

#region Node traits

interface NodeCall : Node
{
    public Identifier Name { get; }
    public IReadOnlyCollection<ParameterActual> Parameters { get; }
}

interface NodeScoped : Node;

interface NodeAliasReference : Node
{
    public Identifier Name { get; }
}

abstract class BlockNode(SourceTokens sourceTokens,
    IReadOnlyCollection<Node.Statement> block)
: NodeImpl(sourceTokens), NodeScoped
{
    public IReadOnlyCollection<Node.Statement> Block => block;
}

interface NodeBracketedExpression : Node.Expression
{
    public Expression ContainedExpression { get; }
}

#endregion Node traits

public interface Node : EquatableSemantics<Node>
{
    SourceTokens SourceTokens { get; }

    public sealed class Algorithm : NodeImpl, NodeScoped
    {
        internal Algorithm(SourceTokens sourceTokens,
            Identifier name,
            IReadOnlyCollection<Declaration> declarations) : base(sourceTokens)
        {
            Name = name;
            Declarations = declarations;
        }

        internal Identifier Name { get; }
        internal IReadOnlyCollection<Declaration> Declarations { get; }

        public override bool SemanticsEqual(Node other) => other is Algorithm o
         && o.Name.SemanticsEqual(Name)
         && o.Declarations.AllSemanticsEqual(Declarations);
    }

    internal interface Declaration : Node
    {
        internal sealed class MainProgram(SourceTokens sourceTokens,
            IReadOnlyCollection<Statement> block)
        : BlockNode(sourceTokens, block), Declaration
        {
            public override bool SemanticsEqual(Node other) => other is MainProgram o
             && o.Block.AllSemanticsEqual(Block);
        }

        internal sealed class TypeAlias(SourceTokens sourceTokens,
            Identifier name,
            Type type)
        : NodeImpl(sourceTokens), Declaration
        {
            public Identifier Name => name;
            public Type Type => type;

            public override bool SemanticsEqual(Node other) => other is TypeAlias o
             && o.Name.SemanticsEqual(Name)
             && o.Type.SemanticsEqual(Type);
        }

        internal sealed class CompleteTypeAlias(SourceTokens sourceTokens,
            Identifier name,
            Type.Complete type)
        : NodeImpl(sourceTokens), Declaration
        {
            public Identifier Name => name;
            public Type.Complete Type => type;

            public override bool SemanticsEqual(Node other) => other is CompleteTypeAlias o
             && o.Name.SemanticsEqual(Name)
             && o.Type.SemanticsEqual(Type);
        }

        internal sealed class Constant(SourceTokens sourceTokens,
            Type type,
            Identifier name,
            Expression value)
        : NodeImpl(sourceTokens), Declaration
        {
            public Type Type => type;
            public Identifier Name => name;
            public Expression Value => value;

            public override bool SemanticsEqual(Node other) => other is Constant o
             && o.Name.SemanticsEqual(Name)
             && o.Type.SemanticsEqual(Type)
             && o.Value.SemanticsEqual(Value);
        }

        internal sealed class Procedure(SourceTokens sourceTokens,
            ProcedureSignature signature)
        : NodeImpl(sourceTokens), Declaration
        {
            public ProcedureSignature Signature => signature;

            public override bool SemanticsEqual(Node other) => other is Procedure o
             && o.Signature.SemanticsEqual(Signature);
        }

        internal sealed class ProcedureDefinition(SourceTokens sourceTokens,
            ProcedureSignature signature,
            IReadOnlyCollection<Statement> block)
        : BlockNode(sourceTokens, block), Declaration
        {
            public ProcedureSignature Signature => signature;

            public override bool SemanticsEqual(Node other) => other is ProcedureDefinition o
             && o.Signature.SemanticsEqual(Signature)
             && o.Block.AllSemanticsEqual(Block);
        }

        internal sealed class Function(SourceTokens sourceTokens,
            FunctionSignature signature)
        : NodeImpl(sourceTokens), Declaration
        {
            public FunctionSignature Signature => signature;

            public override bool SemanticsEqual(Node other) => other is Function o
 && o.Signature.SemanticsEqual(Signature);
        }

        internal sealed class FunctionDefinition(SourceTokens sourceTokens,
            FunctionSignature signature,
            IReadOnlyCollection<Statement> block)
        : BlockNode(sourceTokens, block), Declaration
        {
            public FunctionSignature Signature => signature;

            public override bool SemanticsEqual(Node other) => other is FunctionDefinition o
 && o.Signature.SemanticsEqual(Signature)
 && o.Block.AllSemanticsEqual(Block);
        }
    }

    internal interface Statement : Node
    {
        internal sealed class Nop(SourceTokens sourceTokens)
        : NodeImpl(sourceTokens), Statement
        {
            public override bool SemanticsEqual(Node other) => other is Nop;
        }

        internal sealed class Alternative(SourceTokens sourceTokens,
            Alternative.IfClause @if,
            IReadOnlyCollection<Alternative.ElseIfClause> elseIf,
            Option<Alternative.ElseClause> @else)
        : NodeImpl(sourceTokens), Statement
        {
            public IfClause If => @if;
            public IReadOnlyCollection<ElseIfClause> ElseIfs => @elseIf;
            public Option<ElseClause> Else => @else;

            public override bool SemanticsEqual(Node other) => other is Alternative o
             && o.If.SemanticsEqual(If)
             && o.ElseIfs.AllSemanticsEqual(ElseIfs)
             && o.Else.OptionSemanticsEqual(Else);

            internal sealed class IfClause(SourceTokens sourceTokens,
                Expression condition,
                IReadOnlyCollection<Statement> block)
            : BlockNode(sourceTokens, block)
            {
                public Expression Condition => condition;

                public override bool SemanticsEqual(Node other) => other is IfClause o
                 && o.Condition.SemanticsEqual(Condition)
                 && o.Block.AllSemanticsEqual(Block);
            }

            internal sealed class ElseIfClause(SourceTokens sourceTokens,
                Expression condition,
                IReadOnlyCollection<Statement> block)
            : BlockNode(sourceTokens, block)
            {
                public Expression Condition => condition;

                public override bool SemanticsEqual(Node other) => other is ElseIfClause o
                 && o.Condition.SemanticsEqual(Condition)
                 && o.Block.AllSemanticsEqual(Block);
            }

            internal sealed class ElseClause(SourceTokens sourceTokens,
                IReadOnlyCollection<Statement> block)
            : BlockNode(sourceTokens, block)
            {
                public override bool SemanticsEqual(Node other) => other is ElseClause o
                 && o.Block.AllSemanticsEqual(Block);
            }
        }

        internal sealed class Switch(SourceTokens sourceTokens,
            Expression expression,
            IReadOnlyCollection<Switch.Case> cases,
            Option<Switch.DefaultCase> @default)
        : NodeImpl(sourceTokens), Statement
        {
            public Expression Expression => expression;
            public IReadOnlyCollection<Case> Cases => cases;
            public Option<DefaultCase> Default => @default;

            public override bool SemanticsEqual(Node other) => other is Switch o
                 && o.Expression.SemanticsEqual(Expression)
                 && o.Cases.AllSemanticsEqual(Cases)
                 && o.Default.OptionSemanticsEqual(Default);

            internal sealed class Case(SourceTokens sourceTokens,
                Expression when,
                IReadOnlyCollection<Statement> block)
            : BlockNode(sourceTokens, block)
            {
                public Expression When => when;

                public override bool SemanticsEqual(Node other) => other is Case o
                 && o.When.SemanticsEqual(When)
                 && o.Block.AllSemanticsEqual(Block);
            }

            internal sealed class DefaultCase(SourceTokens sourceTokens,
                IReadOnlyCollection<Statement> block)
            : BlockNode(sourceTokens, block)
            {
                public override bool SemanticsEqual(Node other) => other is DefaultCase o
                 && o.Block.AllSemanticsEqual(Block);
            }
        }

        internal sealed class Assignment(SourceTokens sourceTokens,
            Expression.Lvalue target,
            Expression value)
        : NodeImpl(sourceTokens), Statement
        {
            public Expression.Lvalue Target => target;
            public Expression Value => value;

            public override bool SemanticsEqual(Node other) => other is Assignment o
             && o.Target.SemanticsEqual(Target)
             && o.Value.SemanticsEqual(Value);
        }

        internal sealed class DoWhileLoop(SourceTokens sourceTokens,
            Expression condition,
            IReadOnlyCollection<Statement> block)
        : BlockNode(sourceTokens, block), Statement
        {
            public Expression Condition => condition;

            public override bool SemanticsEqual(Node other) => other is DoWhileLoop o
             && o.Condition.SemanticsEqual(Condition)
                && o.Block.AllSemanticsEqual(Block);
        }

        internal interface Builtin : Statement
        {
            internal sealed class Ecrire(SourceTokens sourceTokens,
                Expression argumentNomLog,
                Expression argumentExpression)
            : NodeImpl(sourceTokens), Builtin
            {
                public Expression ArgumentNomLog => argumentNomLog;
                public Expression ArgumentExpression => argumentExpression;

                public override bool SemanticsEqual(Node other) => other is Ecrire o
                 && o.ArgumentNomLog.SemanticsEqual(ArgumentNomLog)
                 && o.ArgumentExpression.SemanticsEqual(ArgumentExpression);
            }

            internal sealed class Fermer(SourceTokens sourceTokens,
                Expression argumentNomLog)
            : NodeImpl(sourceTokens), Builtin
            {
                public Expression ArgumentNomLog => argumentNomLog;

                public override bool SemanticsEqual(Node other) => other is Fermer o
                 && o.ArgumentNomLog.SemanticsEqual(ArgumentNomLog);
            }

            internal sealed class Lire(SourceTokens sourceTokens,
                Expression argumentNomLog,
                Expression.Lvalue argumentVariable)
            : NodeImpl(sourceTokens), Builtin
            {
                public Expression ArgumentNomLog => argumentNomLog;
                public Expression ArgumentVariable => argumentVariable;

                public override bool SemanticsEqual(Node other) => other is Lire o
                 && o.ArgumentNomLog.SemanticsEqual(ArgumentNomLog)
                 && o.ArgumentVariable.SemanticsEqual(ArgumentVariable);
            }

            internal sealed class OuvrirAjout(SourceTokens sourceTokens,
                Expression argumentNomLog)
            : NodeImpl(sourceTokens), Builtin
            {
                public Expression ArgumentNomLog => argumentNomLog;

                public override bool SemanticsEqual(Node other) => other is OuvrirAjout o
                 && o.ArgumentNomLog.SemanticsEqual(ArgumentNomLog);
            }

            internal sealed class OuvrirEcriture(SourceTokens sourceTokens,
                Expression argumentNomLog)
            : NodeImpl(sourceTokens), Builtin
            {
                public Expression ArgumentNomLog => argumentNomLog;

                public override bool SemanticsEqual(Node other) => other is OuvrirEcriture o
                 && o.ArgumentNomLog.SemanticsEqual(ArgumentNomLog);
            }

            internal sealed class OuvrirLecture(SourceTokens sourceTokens,
                Expression argumentNomLog)
            : NodeImpl(sourceTokens), Builtin
            {
                public Expression ArgumentNomLog => argumentNomLog;

                public override bool SemanticsEqual(Node other) => other is OuvrirLecture o
                 && o.ArgumentNomLog.SemanticsEqual(ArgumentNomLog);
            }

            internal sealed class Assigner(SourceTokens sourceTokens,
                Expression.Lvalue argumentNomLog,
                Expression argumentNomExt)
            : NodeImpl(sourceTokens), Builtin
            {
                public Expression.Lvalue ArgumentNomLog => argumentNomLog;
                public Expression ArgumentNomExt => argumentNomExt;

                public override bool SemanticsEqual(Node other) => other is Assigner o
                 && o.ArgumentNomLog.SemanticsEqual(ArgumentNomLog)
                 && o.ArgumentNomExt.SemanticsEqual(ArgumentNomExt);
            }

            internal sealed class EcrireEcran(SourceTokens sourceTokens,
                IReadOnlyCollection<Expression> arguments)
            : NodeImpl(sourceTokens), Builtin
            {
                public IReadOnlyCollection<Expression> Arguments => arguments;

                public override bool SemanticsEqual(Node other) => other is EcrireEcran o
                 && o.Arguments.AllSemanticsEqual(Arguments);
            }

            internal sealed class LireClavier(SourceTokens sourceTokens,
                Expression.Lvalue argumentVariable)
            : NodeImpl(sourceTokens), Builtin
            {
                public Expression ArgumentVariable => argumentVariable;

                public override bool SemanticsEqual(Node other) => other is LireClavier o
                 && o.ArgumentVariable.SemanticsEqual(ArgumentVariable);
            }
        }

        internal sealed class ForLoop(SourceTokens sourceTokens,
            Expression.Lvalue variant,
            Expression start,
            Expression end,
            Option<Expression> step,
            IReadOnlyCollection<Statement> block)
        : BlockNode(sourceTokens, block), Statement
        {
            public Expression.Lvalue Variant => variant;
            public Expression Start => start;
            public Expression End => end;
            public Option<Expression> Step => step;

            public override bool SemanticsEqual(Node other) => other is ForLoop o
             && o.Variant.SemanticsEqual(Variant)
             && o.Start.SemanticsEqual(Start)
             && o.End.SemanticsEqual(End)
             && o.Step.OptionSemanticsEqual(Step)
             && o.Block.AllSemanticsEqual(Block);
        }

        internal sealed class ProcedureCall(SourceTokens sourceTokens,
            Identifier name,
            IReadOnlyCollection<ParameterActual> parameters)
        : NodeImpl(sourceTokens), Statement, NodeCall
        {
            public Identifier Name => name;
            public IReadOnlyCollection<ParameterActual> Parameters => parameters;

            public override bool SemanticsEqual(Node other) => other is ProcedureCall o
             && o.Name.SemanticsEqual(Name)
             && o.Parameters.AllSemanticsEqual(Parameters);
        }

        internal sealed class RepeatLoop(SourceTokens sourceTokens,
            Expression condition,
            IReadOnlyCollection<Statement> block)
        : BlockNode(sourceTokens, block), Statement
        {
            public Expression Condition => condition;

            public override bool SemanticsEqual(Node other) => other is RepeatLoop o
             && o.Condition.SemanticsEqual(Condition)
             && o.Block.AllSemanticsEqual(Block);
        }

        internal sealed class Return(SourceTokens sourceTokens,
            Expression value)
        : NodeImpl(sourceTokens), Statement
        {
            public Expression Value => value;

            public override bool SemanticsEqual(Node other) => other is Return o
             && o.Value.SemanticsEqual(Value);
        }

        internal sealed class LocalVariable(SourceTokens sourceTokens,
            IReadOnlyCollection<Identifier> names,
            Type.Complete type)
        : NodeImpl(sourceTokens), Statement
        {
            public IReadOnlyCollection<Identifier> Names => names;
            public Type Type => type;

            public override bool SemanticsEqual(Node other) => other is LocalVariable o
             && o.Names.AllSemanticsEqual(Names)
             && o.Type.SemanticsEqual(Type);
        }

        internal sealed class WhileLoop(SourceTokens sourceTokens,
            Expression condition,
            IReadOnlyCollection<Statement> block)
        : BlockNode(sourceTokens, block), Statement
        {
            public Expression Condition => condition;

            public override bool SemanticsEqual(Node other) => other is WhileLoop o
             && o.Condition.SemanticsEqual(Condition)
             && o.Block.AllSemanticsEqual(Block);
        }
    }

    internal interface Expression : Node
    {
        internal interface Lvalue : Expression
        {
            internal sealed class ComponentAccess(SourceTokens sourceTokens,
                Expression structure,
                Identifier componentName)
            : NodeImpl(sourceTokens), Lvalue
            {
                public Expression Structure => structure;
                public Identifier ComponentName => componentName;

                public override bool SemanticsEqual(Node other) => other is ComponentAccess o
                 && o.Structure.SemanticsEqual(Structure)
                 && o.ComponentName.SemanticsEqual(ComponentName);
            }

            internal sealed new class Bracketed(SourceTokens sourceTokens,
                Lvalue lvalue)
            : NodeImpl(sourceTokens), Lvalue, NodeBracketedExpression
            {
                public Lvalue ContainedLvalue { get; } = lvalue is NodeBracketedExpression b
                                              && b.ContainedExpression is Lvalue l
                                              ? l : lvalue;

                Expression NodeBracketedExpression.ContainedExpression => ContainedLvalue;

                public override bool SemanticsEqual(Node other) => other is Bracketed o
                 && o.ContainedLvalue.SemanticsEqual(ContainedLvalue);
            }

            internal sealed class ArraySubscript(SourceTokens sourceTokens,
                Expression array,
                IReadOnlyCollection<Expression> indexes)
            : NodeImpl(sourceTokens), Lvalue
            {
                public Expression Array => array;
                public IReadOnlyCollection<Expression> Indexes => indexes;

                public override bool SemanticsEqual(Node other) => other is ArraySubscript o
                 && o.Array.SemanticsEqual(Array)
                 && o.Indexes.AllSemanticsEqual(Indexes);
            }

            internal sealed class VariableReference(SourceTokens sourceTokens,
                Identifier name)
            : NodeImpl(sourceTokens), Lvalue
            {
                public Identifier Name => name;

                public override bool SemanticsEqual(Node other) => other is VariableReference o
                 && o.Name.SemanticsEqual(Name);
            }
        }

        internal sealed class UnaryOperation(SourceTokens sourceTokens,
            UnaryOperator @operator,
            Expression operand)
        : NodeImpl(sourceTokens), Expression
        {
            public UnaryOperator Operator => @operator;
            public Expression Operand => operand;

            public override bool SemanticsEqual(Node other) => other is UnaryOperation o
             && o.Operator == Operator
             && o.Operand.SemanticsEqual(Operand);
        }

        internal sealed class BinaryOperation(SourceTokens sourceTokens,
            Expression left,
            BinaryOperator @operator,
            Expression right)
        : NodeImpl(sourceTokens), Expression
        {
            public Expression Left => left;
            public BinaryOperator Operator => @operator;
            public Expression Right => right;

            public override bool SemanticsEqual(Node other) => other is BinaryOperation o
             && o.Left.SemanticsEqual(Left)
             && o.Operator == Operator
             && o.Right.SemanticsEqual(Right);
        }

        internal sealed class BuiltinFdf(SourceTokens sourceTokens,
            Expression argumentNomLog)
        : NodeImpl(sourceTokens), Expression
        {
            public Expression ArgumentNomLog => argumentNomLog;

            public override bool SemanticsEqual(Node other) => other is BuiltinFdf o
             && o.ArgumentNomLog.SemanticsEqual(ArgumentNomLog);
        }

        internal sealed class FunctionCall(SourceTokens sourceTokens,
            Identifier name,
            IReadOnlyCollection<ParameterActual> parameters)
        : NodeImpl(sourceTokens), Expression, NodeCall
        {
            public Identifier Name => name;
            public IReadOnlyCollection<ParameterActual> Parameters => parameters;

            public override bool SemanticsEqual(Node other) => other is FunctionCall o
             && o.Name.SemanticsEqual(Name)
             && o.Parameters.AllSemanticsEqual(Parameters);
        }

        internal sealed class Bracketed(SourceTokens sourceTokens,
            Expression expression)
        : NodeImpl(sourceTokens), NodeBracketedExpression
        {
            public Expression ContainedExpression { get; } = expression is NodeBracketedExpression b
                                                           ? b.ContainedExpression : expression;

            public override bool SemanticsEqual(Node other) => other is Bracketed o
             && o.ContainedExpression.SemanticsEqual(ContainedExpression);
        }

        internal abstract class Literal<TValue, TUnderlying>(SourceTokens sourceTokens, TValue value)
            : NodeImpl(sourceTokens), Literal
            where TValue : Value<TValue, TUnderlying>
            where TUnderlying : IConvertible, IEquatable<TUnderlying>
        {
            // The provided Value for a literal must always have a value
            public TUnderlying Value { get; } = value.Value.Value.NotNull();
            public TValue EvaluatedValue { get; } = value;
            IConvertible Literal.Value => Value;
            Value Literal.EvaluatedValue => EvaluatedValue;

            public override bool SemanticsEqual(Node other) => other is Literal<TValue, TUnderlying> o
             && o.Value.Equals(Value);
        }

        internal interface Literal : Expression
        {
            IConvertible Value { get; }
            Value EvaluatedValue { get; }
            internal sealed class True(SourceTokens sourceTokens)
            : Literal<Value.Boolean, bool>(sourceTokens, new(true))
            {
            }

            internal sealed class False(SourceTokens sourceTokens)
            : Literal<Value.Boolean, bool>(sourceTokens, new(false))
            {
            }

            internal sealed class Character(SourceTokens sourceTokens, char value)
            : Literal<Value.Character, char>(sourceTokens, new(value))
            {
                public Character(SourceTokens sourceTokens, string valueStr)
                : this(sourceTokens, char.Parse(valueStr)) { }
            }

            internal sealed class Integer(SourceTokens sourceTokens, int value)
            : Literal<Value.Integer, int>(sourceTokens, new(value))
            {
                public Integer(SourceTokens sourceTokens, string valueStr)
                : this(sourceTokens, int.Parse(valueStr, CultureInfo.InvariantCulture)) { }
            }

            internal sealed class Real(SourceTokens sourceTokens, decimal value)
            : Literal<Value.Real, decimal>(sourceTokens, new(value))
            {
                public Real(SourceTokens sourceTokens, string valueStr)
                : this(sourceTokens, decimal.Parse(valueStr, CultureInfo.InvariantCulture)) { }
            };

            internal sealed class String(SourceTokens sourceTokens,
                string value)
            : Literal<Value.String, string>(sourceTokens, new(value));
        }
    }

    internal interface Type : Node
    {
        internal sealed class AliasReference(SourceTokens sourceTokens,
            Identifier name)
        : NodeImpl(sourceTokens), Type, NodeAliasReference
        {
            public Identifier Name => name;

            public override bool SemanticsEqual(Node other) => other is AliasReference o
             && o.Name.SemanticsEqual(Name);
        }

        internal sealed class String(SourceTokens sourceTokens)
        : NodeImpl(sourceTokens), Type
        {
            public override bool SemanticsEqual(Node other) => other is String;
        }

        internal interface Complete : Type
        {
            internal sealed class Array(SourceTokens sourceTokens,
                Type type,
                IReadOnlyCollection<Expression> dimensions)
            : NodeImpl(sourceTokens), Complete
            {
                public Type Type => type;
                public IReadOnlyCollection<Expression> Dimensions => dimensions;

                public override bool SemanticsEqual(Node other) => other is Array o
                 && o.Type.SemanticsEqual(Type)
                 && o.Dimensions.AllSemanticsEqual(Dimensions);
            }

            internal sealed class File(SourceTokens sourceTokens)
            : NodeImpl(sourceTokens), Complete
            {
                public override bool SemanticsEqual(Node other) => other is File;
            }

            internal sealed class Character(SourceTokens sourceTokens)
             : NodeImpl(sourceTokens), Complete
            {
                public override bool SemanticsEqual(Node other) => other is Character;
            }

            internal sealed class Boolean(SourceTokens sourceTokens)
             : NodeImpl(sourceTokens), Complete
            {
                public override bool SemanticsEqual(Node other) => other is Boolean;
            }

            internal sealed class Integer(SourceTokens sourceTokens)
             : NodeImpl(sourceTokens), Complete
            {
                public override bool SemanticsEqual(Node other) => other is Integer;
            }

            internal sealed class Real(SourceTokens sourceTokens)
             : NodeImpl(sourceTokens), Complete
            {
                public override bool SemanticsEqual(Node other) => other is Real;
            }

            internal sealed new class AliasReference(SourceTokens sourceTokens,
                Identifier name)
            : NodeImpl(sourceTokens), Complete, NodeAliasReference
            {
                public Identifier Name => name;

                public override bool SemanticsEqual(Node other) => other is AliasReference o
                 && o.Name.SemanticsEqual(Name);
            }

            internal sealed class LengthedString(SourceTokens sourceTokens,
                Expression length)
            : NodeImpl(sourceTokens), Complete
            {
                public Expression Length => length;

                public override bool SemanticsEqual(Node other) => other is LengthedString o
                 && o.Length.SemanticsEqual(Length);
            }

            internal sealed class Structure(SourceTokens sourceTokens,
                IReadOnlyCollection<Statement.LocalVariable> components)
            : NodeImpl(sourceTokens), Complete
            {
                public IReadOnlyCollection<Statement.LocalVariable> Components => components;

                public override bool SemanticsEqual(Node other) => other is Structure o
                 && o.Components.AllSemanticsEqual(Components);
            }
        }
    }

    internal sealed class ParameterActual(SourceTokens sourceTokens,
        ParameterMode mode,
        Expression value)
    : NodeImpl(sourceTokens)
    {
        public ParameterMode Mode => mode;
        public Expression Value => value;

        public override bool SemanticsEqual(Node other) => other is ParameterActual o
         && o.Mode == Mode
         && o.Value.SemanticsEqual(Value);
    }

    internal sealed class ParameterFormal(SourceTokens sourceTokens,
        ParameterMode mode,
        Identifier name,
        Type type)
    : NodeImpl(sourceTokens)
    {
        public ParameterMode Mode => mode;
        public Identifier Name => name;
        public Type Type => type;

        public override bool SemanticsEqual(Node other) => other is ParameterFormal o
         && o.Mode == Mode
         && o.Name.SemanticsEqual(Name)
         && o.Type.SemanticsEqual(Type);
    }

    internal sealed class ProcedureSignature(SourceTokens sourceTokens,
        Identifier name,
        IReadOnlyCollection<ParameterFormal> parameters)
    : NodeImpl(sourceTokens)
    {
        public Identifier Name => name;
        public IReadOnlyCollection<ParameterFormal> Parameters => parameters;

        public override bool SemanticsEqual(Node other) => other is ProcedureSignature o
         && o.Name.SemanticsEqual(Name)
         && o.Parameters.AllSemanticsEqual(Parameters);
    }

    internal sealed class FunctionSignature(SourceTokens sourceTokens,
        Identifier name,
        IReadOnlyCollection<ParameterFormal> parameters,
        Type returnType)
    : NodeImpl(sourceTokens)
    {
        public Identifier Name => name;
        public IReadOnlyCollection<ParameterFormal> Parameters => parameters;
        public Type ReturnType => returnType;

        public override bool SemanticsEqual(Node other) => other is FunctionSignature o
         && o.Name.SemanticsEqual(Name)
         && o.Parameters.AllSemanticsEqual(Parameters)
         && o.ReturnType.SemanticsEqual(ReturnType);
    }
}

#region Terminals

sealed class ParameterMode
{
    ParameterMode(string formal, string actual)
     => (RepresentationFormal, RepresentationActual) = (formal, actual);

    public string RepresentationFormal { get; }
    public string RepresentationActual { get; }

    public static ParameterMode In { get; } = new("entF", "entE");
    public static ParameterMode Out { get; } = new("sortF", "sortE");
    public static ParameterMode InOut { get; } = new("entF/sortF", "entE/sortE");
}

sealed class Identifier : NodeImpl, IEquatable<Identifier?>
{
    public Identifier(SourceTokens sourceTokens, string name) : base(sourceTokens)
    {
        Name = name;
#if DEBUG
        if (!IsValidIdentifier(name)) {
            throw new ArgumentException($"`{name}` is not a valid identifier", nameof(name));
        }
        static bool IsValidIdentifier(string name) => name.Length > 0
            && (name[0] == '_' || char.IsLetter(name[0]))
            && name.Skip(1).All(c => c == '_' || char.IsLetterOrDigit(c));
#endif
    }

    public string Name { get; }

    public override bool SemanticsEqual(Node other) => other is Identifier o
     && o.Name == Name;

    // Equals and GetHashCode implementation for usage in dictionaries.
    public override bool Equals(object? obj) => Equals(obj as Identifier);

    public bool Equals(Identifier? other) => other is not null && other.Name == Name;

    public override int GetHashCode() => Name.GetHashCode();
}

#endregion Terminals

public abstract class NodeImpl(SourceTokens sourceTokens) : Node
{
    public SourceTokens SourceTokens => sourceTokens;

    public abstract bool SemanticsEqual(Node other);
}
