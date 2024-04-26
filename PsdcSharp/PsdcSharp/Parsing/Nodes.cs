using Scover.Psdc.Tokenization;

namespace Scover.Psdc.Parsing.Nodes;

internal interface CallNode : Node
{
    public Identifier Name { get; }
    public IReadOnlyCollection<EffectiveParameter> Parameters { get; }
}

internal abstract class NodeImpl(Partition<Token> sourceTokens) : Node
{
    public Partition<Token> SourceTokens => sourceTokens;
}

internal abstract class ScopedNode(Partition<Token> sourceTokens) : NodeImpl(sourceTokens);

internal abstract class BlockNode(Partition<Token> sourceTokens,
    IReadOnlyCollection<Node.Statement> block)
: ScopedNode(sourceTokens)
{
    public IReadOnlyCollection<Node.Statement> Block => block;
}

internal interface Node
{
    Partition<Token> SourceTokens { get; }
    internal sealed class Algorithm(Partition<Token> sourceTokens,
        Identifier name,
        IReadOnlyCollection<Declaration> declarations)
    : ScopedNode(sourceTokens)
    {
        public Identifier Name => name;
        public IReadOnlyCollection<Declaration> Declarations => declarations;
    }

    internal interface Declaration : Node
    {
        internal sealed class MainProgram(Partition<Token> sourceTokens,
            IReadOnlyCollection<Statement> block)
        : BlockNode(sourceTokens, block), Declaration;

        internal sealed class TypeAlias(Partition<Token> sourceTokens,
            Identifier name,
            Type type)
        : NodeImpl(sourceTokens), Declaration
        {
            public Identifier Name => name;
            public Type Type => type;
        }

        internal sealed class CompleteTypeAlias(Partition<Token> sourceTokens,
            Identifier name,
            Type.Complete type)
        : NodeImpl(sourceTokens), Declaration
        {
            public Identifier Name => name;
            public Type.Complete Type => type;
        }

        internal sealed class Constant(Partition<Token> sourceTokens,
            Type type,
            Identifier name,
            Expression value)
        : NodeImpl(sourceTokens), Declaration
        {
            public Type Type => type;
            public Identifier Name => name;
            public Expression Value => value;
        }

        internal sealed class Procedure(Partition<Token> sourceTokens,
            ProcedureSignature signature)
        : NodeImpl(sourceTokens), Declaration
        {
            public ProcedureSignature Signature => signature;
        }

        internal sealed class ProcedureDefinition(Partition<Token> sourceTokens,
            ProcedureSignature signature,
            IReadOnlyCollection<Statement> block)
        : BlockNode(sourceTokens, block), Declaration
        {
            public ProcedureSignature Signature => signature;
        }

        internal sealed class Function(Partition<Token> sourceTokens,
            FunctionSignature signature)
        : NodeImpl(sourceTokens), Declaration
        {
            public FunctionSignature Signature => signature;
        }

        internal sealed class FunctionDefinition(Partition<Token> sourceTokens,
            FunctionSignature signature,
            IReadOnlyCollection<Statement> block)
        : BlockNode(sourceTokens, block), Declaration
        {
            public FunctionSignature Signature => signature;
        }
    }

    internal interface Statement : Node
    {
        internal sealed class Nop(Partition<Token> sourceTokens)
        : NodeImpl(sourceTokens), Statement;

        internal sealed class Alternative(Partition<Token> sourceTokens,
            Alternative.IfClause @if,
            IReadOnlyCollection<Alternative.ElseIfClause> elseIf,
            Option<Alternative.ElseClause> @else)
        : NodeImpl(sourceTokens), Statement
        {
            public IfClause If => @if;
            public IReadOnlyCollection<ElseIfClause> ElseIfs => @elseIf;
            public Option<ElseClause> Else => @else;

            internal sealed class IfClause(Partition<Token> sourceTokens,
                Expression condition,
                IReadOnlyCollection<Statement> block)
            : BlockNode(sourceTokens, block)
            {
                public Expression Condition => condition;
            }

            internal sealed class ElseIfClause(Partition<Token> sourceTokens,
                Expression condition,
                IReadOnlyCollection<Statement> block)
            : BlockNode(sourceTokens, block)
            {
                public Expression Condition => condition;
            }

            internal sealed class ElseClause(Partition<Token> sourceTokens,
                IReadOnlyCollection<Statement> block)
            : BlockNode(sourceTokens, block);
        }

        internal sealed class Switch(Partition<Token> sourceTokens,
            Expression expression,
            IReadOnlyCollection<Switch.Case> cases,
            Option<Switch.CaseDefault> @default)
        : NodeImpl(sourceTokens), Statement
        {
            public Expression Expression => expression;
            public IReadOnlyCollection<Case> Cases => cases;
            public Option<CaseDefault> Default => @default;

            internal sealed class Case(Partition<Token> sourceTokens,
                Expression when,
                IReadOnlyCollection<Statement> block)
            : BlockNode(sourceTokens, block)
            {
                public Expression When => when;
            }

            internal sealed class CaseDefault(Partition<Token> sourceTokens,
                IReadOnlyCollection<Statement> block)
            : BlockNode(sourceTokens, block);
        }

        internal sealed class Assignment(Partition<Token> sourceTokens,
            Expression.LValue target,
            Expression value)
        : NodeImpl(sourceTokens), Statement
        {
            public Expression.LValue Target => target;
            public Expression Value => value;
        }

        internal sealed class DoWhileLoop(Partition<Token> sourceTokens,
            Expression condition,
            IReadOnlyCollection<Statement> block)
        : BlockNode(sourceTokens, block), Statement
        {
            public Expression Condition => condition;
        }

        internal sealed class BuiltinEcrire(Partition<Token> sourceTokens,
            Expression argumentNomLog,
            Expression argumentExpression)
        : NodeImpl(sourceTokens), Statement
        {
            public Expression ArgumentNomLog => argumentNomLog;
            public Expression ArgumentExpression => argumentExpression;
        }

        internal sealed class BuiltinFermer(Partition<Token> sourceTokens,
            Expression argumentNomLog)
        : NodeImpl(sourceTokens), Statement
        {
            public Expression ArgumentNomLog => argumentNomLog;
        }

        internal sealed class ForLoop(Partition<Token> sourceTokens,
            Expression.LValue variant,
            Expression start,
            Expression end,
            Option<Expression> step,
            IReadOnlyCollection<Statement> block)
        : BlockNode(sourceTokens, block), Statement
        {
            public Expression.LValue Variant => variant;
            public Expression Start => start;
            public Expression End => end;
            public Option<Expression> Step => step;
        }

        internal sealed class BuiltinLire(Partition<Token> sourceTokens,
            Expression argumentNomLog,
            Expression.LValue argumentVariable)
        : NodeImpl(sourceTokens), Statement
        {
            public Expression ArgumentNomLog => argumentNomLog;
            public Expression ArgumentVariable => argumentVariable;
        }

        internal sealed class BuiltinOuvrirAjout(Partition<Token> sourceTokens,
            Expression argumentNomLog)
        : NodeImpl(sourceTokens), Statement
        {
            public Expression ArgumentNomLog => argumentNomLog;
        }

        internal sealed class BuiltinOuvrirEcriture(Partition<Token> sourceTokens,
            Expression argumentNomLog)
        : NodeImpl(sourceTokens), Statement
        {
            public Expression ArgumentNomLog => argumentNomLog;
        }

        internal sealed class BuiltinOuvrirLecture(Partition<Token> sourceTokens,
            Expression argumentNomLog)
        : NodeImpl(sourceTokens), Statement
        {
            public Expression ArgumentNomLog => argumentNomLog;
        }

        internal sealed class ProcedureCall(Partition<Token> sourceTokens,
            Identifier name,
            IReadOnlyCollection<EffectiveParameter> parameters)
        : NodeImpl(sourceTokens), Statement, CallNode
        {
            public Identifier Name => name;
            public IReadOnlyCollection<EffectiveParameter> Parameters => parameters;
        }

        internal sealed class BuiltinAssigner(Partition<Token> sourceTokens,
            Expression.LValue argumentNomLog,
            Expression argumentNomExt)
        : NodeImpl(sourceTokens), Statement
        {
            public Expression.LValue ArgumentNomLog => argumentNomLog;
            public Expression ArgumentNomExt => argumentNomExt;
        }

        internal sealed class BuiltinEcrireEcran(Partition<Token> sourceTokens,
            IReadOnlyCollection<Expression> arguments)
        : NodeImpl(sourceTokens), Statement
        {
            public IReadOnlyCollection<Expression> Arguments => arguments;
        }

        internal sealed class BuiltinLireClavier(Partition<Token> sourceTokens,
            Expression.LValue argumentVariable)
        : NodeImpl(sourceTokens), Statement
        {
            public Expression ArgumentVariable => argumentVariable;
        }

        internal sealed class RepeatLoop(Partition<Token> sourceTokens,
            Expression condition,
            IReadOnlyCollection<Statement> block)
        : BlockNode(sourceTokens, block), Statement
        {
            public Expression Condition => condition;
        }

        internal sealed class Return(Partition<Token> sourceTokens,
            Expression value)
        : NodeImpl(sourceTokens), Statement
        {
            public Expression Value => value;
        }

        internal sealed class LocalVariable(Partition<Token> sourceTokens,
            IReadOnlyCollection<Identifier> names,
            Type.Complete type)
        : NodeImpl(sourceTokens), Statement, IEquatable<LocalVariable?>
        {
            public IReadOnlyCollection<Identifier> Names => names;
            public Type Type => type;

            public override bool Equals(object? obj) => Equals(obj as LocalVariable);
            public bool Equals(LocalVariable? other) => other is not null
             && other.Names.SequenceEqual(Names)
             && other.Type.Equals(Type);
            public override int GetHashCode() => HashCode.Combine(Names.GetSequenceHashCode(), Type);
        }
        internal sealed class WhileLoop(Partition<Token> sourceTokens,
            Expression condition,
            IReadOnlyCollection<Statement> block)
        : BlockNode(sourceTokens, block), Statement
        {
            public Expression Condition => condition;
        }
    }

    internal abstract class Expression(Partition<Token> sourceTokens) : NodeImpl(sourceTokens), IEquatable<Expression?>
    {
        public override bool Equals(object? obj) => Equals(obj as Expression);
        public abstract bool Equals(Expression? other);
        public abstract override int GetHashCode();

        internal abstract class LValue(Partition<Token> sourceTokens) : Expression(sourceTokens)
        {
            internal sealed class ComponentAccess(Partition<Token> sourceTokens,
                Expression structure,
                Identifier componentName)
            : LValue(sourceTokens)
            {
                public Expression Structure => structure;
                public Identifier ComponentName => componentName;

                public override bool Equals(Expression? other) => other is ComponentAccess o
                 && o.Structure.Equals(Structure)
                 && o.ComponentName.Equals(ComponentName);
                public override int GetHashCode() => HashCode.Combine(Structure, ComponentName);
            }

            internal sealed new class Bracketed(Partition<Token> sourceTokens,
                LValue lvalue)
            : LValue(sourceTokens)
            {
                public new LValue LValue => lvalue;

                public override bool Equals(Expression? other) => other is Bracketed o
                 && o.LValue.Equals(LValue);

                public override int GetHashCode() => LValue.GetHashCode();
            }

            internal sealed class ArraySubscript(Partition<Token> sourceTokens,
                Expression array,
                IReadOnlyCollection<Expression> indices)
            : LValue(sourceTokens)
            {
                public Expression Array => array;
                public IReadOnlyCollection<Expression> Indexes => indices;

                public override bool Equals(Expression? other) => other is ArraySubscript o
                 && o.Array.Equals(Array)
                 && o.Indexes.SequenceEqual(Indexes);
                public override int GetHashCode() => HashCode.Combine(Array, Indexes.GetSequenceHashCode());
            }

            internal sealed class VariableReference(Partition<Token> sourceTokens,
                Identifier name)
            : LValue(sourceTokens)
            {
                public Identifier Name => name;

                public override bool Equals(Expression? other) => other is VariableReference o
                 && o.Name.Equals(Name);
                public override int GetHashCode() => Name.GetHashCode();
            }
        }

        internal sealed class OperationUnary(Partition<Token> sourceTokens,
            UnaryOperator @operator,
            Expression operand)
        : Expression(sourceTokens)
        {
            public UnaryOperator Operator => @operator;
            public Expression Operand => operand;

            public override bool Equals(Expression? other) => other is OperationUnary o
             && o.Operator.Equals(Operator)
             && o.Operand.Equals(Operand);
            public override int GetHashCode() => HashCode.Combine(Operator, Operand);
        }

        internal sealed class OperationBinary(Partition<Token> sourceTokens,
            Expression operand1,
            BinaryOperator @operator,
            Expression operand2)
        : Expression(sourceTokens)
        {
            public Expression Operand1 => operand1;
            public BinaryOperator Operator => @operator;
            public Expression Operand2 => operand2;

            public override bool Equals(Expression? other) => other is OperationBinary o
             && o.Operand1.Equals(Operand1)
             && o.Operator.Equals(Operator)
             && o.Operand2.Equals(Operand2);
            public override int GetHashCode() => HashCode.Combine(Operand1, Operator, Operand2);
        }

        internal sealed class BuiltinFdf(Partition<Token> sourceTokens,
            Expression argumentNomLog)
        : Expression(sourceTokens)
        {
            public Expression ArgumentNomLog => argumentNomLog;

            public override bool Equals(Expression? other) => other is BuiltinFdf o
             && o.ArgumentNomLog.Equals(ArgumentNomLog);
            public override int GetHashCode() => ArgumentNomLog.GetHashCode();
        }

        internal sealed class FunctionCall(Partition<Token> sourceTokens,
            Identifier name,
            IReadOnlyCollection<EffectiveParameter> parameters)
        : Expression(sourceTokens), CallNode
        {
            public Identifier Name => name;
            public IReadOnlyCollection<EffectiveParameter> Parameters => parameters;

            public override bool Equals(Expression? other) => other is FunctionCall o
             && o.Name.Equals(Name)
             && o.Parameters.SequenceEqual(Parameters);
            public override int GetHashCode() => HashCode.Combine(Name, Parameters.GetSequenceHashCode());
        }

        internal sealed class Bracketed(Partition<Token> sourceTokens,
            Expression expression)
        : Expression(sourceTokens)
        {
            public Expression Expression => expression;

            public override bool Equals(Expression? other) => other is Bracketed o
             && o.Expression.Equals(Expression);
            public override int GetHashCode() => Expression.GetHashCode();
        }

        internal abstract class Literal : Expression
        {
            private Literal(Partition<Token> sourceTokens, string value) : base(sourceTokens)
             => Value = value;

            public string Value { get; }

            public override bool Equals(Expression? other) => other is Literal o
             && o.Value.Equals(Value);
            public override int GetHashCode() => Value.GetHashCode();

            internal sealed class True(Partition<Token> sourceTokens)
            : Literal(sourceTokens, "vrai");

            internal sealed class False(Partition<Token> sourceTokens)
            : Literal(sourceTokens, "faux");

            internal sealed class Character(Partition<Token> sourceTokens,
                string Value)
            : Literal(sourceTokens, Value);

            internal sealed class Integer(Partition<Token> sourceTokens,
                string Value)
            : Literal(sourceTokens, Value);

            internal sealed class Real(Partition<Token> sourceTokens,
                string Value)
            : Literal(sourceTokens, Value);

            internal sealed class String(Partition<Token> sourceTokens,
                string Value)
            : Literal(sourceTokens, Value);
        }

    }

    internal abstract class Type(Partition<Token> sourceTokens) : NodeImpl(sourceTokens), IEquatable<Type?>
    {
        public override bool Equals(object? obj) => Equals(obj as Type);
        public abstract bool Equals(Type? other);
        public abstract override int GetHashCode();

        internal sealed class AliasReference(Partition<Token> sourceTokens,
            Identifier name)
        : Complete(sourceTokens)
        {
            public Identifier Name => name;
            public override bool Equals(Type? other) => other is AliasReference o
             && o.Name.Equals(Name);
            public override int GetHashCode() => Name.GetHashCode();
        }

        internal sealed class String(Partition<Token> sourceTokens)
        : Type(sourceTokens)
        {
            public override bool Equals(Type? other) => true;
            public override int GetHashCode() => 0;
        }

        internal abstract class Complete(Partition<Token> sourceTokens)
        : Type(sourceTokens)
        {
            internal sealed class Array(Partition<Token> sourceTokens,
                Type type,
                IReadOnlyCollection<Expression> dimensions)
            : Complete(sourceTokens)
            {
                public Type Type => type;
                public IReadOnlyCollection<Expression> Dimensions => dimensions;
                public override bool Equals(Type? other) => other is Array o
                 && o.Type.Equals(Type)
                 && o.Dimensions.SequenceEqual(o.Dimensions);

                public override int GetHashCode() => HashCode.Combine(Type, Dimensions.GetSequenceHashCode());
            }

            internal sealed class Primitive(Partition<Token> sourceTokens,
                PrimitiveType type)
            : Complete(sourceTokens)
            {
                public PrimitiveType Type => type;
                public override bool Equals(Type? other) => other is Primitive o
                 && o.Type.Equals(Type);
                public override int GetHashCode() => Type.GetHashCode();
            }

            internal sealed new class AliasReference(Partition<Token> sourceTokens,
                Identifier name)
            : Complete(sourceTokens)
            {
                public Identifier Name => name;
                public override bool Equals(Type? other) => other is AliasReference o
                 && o.Name.Equals(Name);
                public override int GetHashCode() => Name.GetHashCode();
            }

            internal sealed class LengthedString(Partition<Token> sourceTokens,
                Expression length)
            : Complete(sourceTokens)
            {
                public Expression Length => length;
                public override bool Equals(Type? other) => other is LengthedString o
                 && o.Length.Equals(length);
                public override int GetHashCode() => Length.GetHashCode();
            }

            internal sealed class Structure(Partition<Token> sourceTokens,
                IReadOnlyCollection<Statement.LocalVariable> components)
            : Complete(sourceTokens)
            {
                public IReadOnlyCollection<Statement.LocalVariable> Components => components;
                public override bool Equals(Type? other) => other is Structure o
                 && o.Components.SequenceEqual(Components);
                public override int GetHashCode() => Components.GetSequenceHashCode();
            }
        }
    }

    #region Other

    internal sealed class EffectiveParameter(Partition<Token> sourceTokens,
        ParameterMode mode,
        Expression value)
    : NodeImpl(sourceTokens)
    {
        public ParameterMode Mode => mode;
        public Expression Value => value;
    }

    internal sealed class FormalParameter(Partition<Token> sourceTokens,
        ParameterMode mode,
        Identifier name,
        Type type)
    : NodeImpl(sourceTokens), IEquatable<FormalParameter?>
    {
        public ParameterMode Mode => mode;
        public Identifier Name => name;
        public Type Type => type;

        public override bool Equals(object? obj) => Equals(obj as FormalParameter);
        public bool Equals(FormalParameter? other) => other is not null
         && other.Mode.Equals(Mode)
         && other.Name.Equals(Name)
         && other.Type.Equals(Type);
        public override int GetHashCode() => HashCode.Combine(Mode, Name, Type);
    }

    internal sealed class ProcedureSignature(Partition<Token> sourceTokens,
        Identifier name,
        IReadOnlyCollection<FormalParameter> parameters)
    : NodeImpl(sourceTokens), IEquatable<ProcedureSignature?>
    {
        public Identifier Name => name;
        public IReadOnlyCollection<FormalParameter> Parameters => parameters;

        public override bool Equals(object? other) => Equals(other as ProcedureSignature);
        public bool Equals(ProcedureSignature? other) => other is not null
            && other.Name.Equals(Name)
            && other.Parameters.SequenceEqual(Parameters);

        public override int GetHashCode() => HashCode.Combine(Name, Parameters.GetSequenceHashCode());
    }

    internal sealed class FunctionSignature(Partition<Token> sourceTokens,
        Identifier name,
        IReadOnlyCollection<FormalParameter> parameters,
        Type returnType)
    : NodeImpl(sourceTokens), IEquatable<FunctionSignature?>
    {
        public Identifier Name => name;
        public IReadOnlyCollection<FormalParameter> Parameters => parameters;
        public Type ReturnType => returnType;

        public override bool Equals(object? other) => Equals(other as FunctionSignature);
        public bool Equals(FunctionSignature? other) => other is not null
            && other.Name.Equals(Name)
            && other.Parameters.SequenceEqual(Parameters)
            && other.ReturnType.Equals(ReturnType);

        public override int GetHashCode() => HashCode.Combine(Name, Parameters.GetSequenceHashCode(), ReturnType);
    }

    internal sealed class Identifier : NodeImpl, IEquatable<Identifier?>
    {
        public Identifier(Partition<Token> sourceTokens, string name) : base(sourceTokens)
        {
#if DEBUG
            if (!IsValidIdentifier(name)) {
                throw new ArgumentException($"`{name}` is not a valid identifier", nameof(name));
            }
#endif
            Name = name;
        }

        public string Name { get; }

        public override bool Equals(object? obj) => Equals(obj as Identifier);
        public override string ToString() => Name;
        public bool Equals(Identifier? other) => other is not null
         && other.Name.Equals(Name);

        public override int GetHashCode() => Name.GetHashCode();

        private static bool IsValidIdentifier(string name) => name.Length > 0
             && (name[0] == '_' || char.IsLetter(name[0]))
             && name.Skip(1).All(c => c == '_' || char.IsLetterOrDigit(c));

    }

    #endregion Other
}

#region Terminals

internal enum ParameterMode
{
    In,
    Out,
    InOut,
}

internal enum PrimitiveType
{
    Boolean,
    Character,
    Integer,
    File,
    Real,
}

#endregion Terminals
