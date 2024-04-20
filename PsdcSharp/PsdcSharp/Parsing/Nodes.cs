using Scover.Psdc.Tokenization;

namespace Scover.Psdc.Parsing.Nodes;

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
        IReadOnlyCollection<Declaration> declarations)
    : ScopedNode(sourceTokens)
    {
        public IReadOnlyCollection<Declaration> Declarations => declarations;
    }

    internal interface Declaration : Node
    {
        internal sealed class MainProgram(Partition<Token> sourceTokens,
            string programName,
            IReadOnlyCollection<Statement> block)
        : BlockNode(sourceTokens, block), Declaration
        {
            public string ProgramName => programName;
        }

        internal sealed class Alias(Partition<Token> sourceTokens,
            string name,
            Type type)
        : NodeImpl(sourceTokens), Declaration
        {
            public string Name => name;
            public Type Type => type;
        }

        internal sealed class Constant(Partition<Token> sourceTokens,
            string name,
            Expression value)
        : NodeImpl(sourceTokens), Declaration
        {
            public string Name => name;
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

        internal sealed class Assignment(Partition<Token> sourceTokens,
            string target,
            Expression value)
        : NodeImpl(sourceTokens), Statement
        {
            public string Target => target;
            public Expression Value => value;
        }

        internal sealed class DoWhileLoop(Partition<Token> sourceTokens,
            Expression condition,
            IReadOnlyCollection<Statement> block)
        : BlockNode(sourceTokens, block), Statement
        {
            public Expression Condition => condition;
        }

        internal sealed class Ecrire(Partition<Token> sourceTokens,
            Expression argument1,
            Expression argument2)
        : NodeImpl(sourceTokens), Statement
        {
            public Expression Argument1 => argument1;
            public Expression Argument2 => argument2;
        }

        internal sealed class Fermer(Partition<Token> sourceTokens,
            Expression argument)
        : NodeImpl(sourceTokens), Statement
        {
            public Expression Argument => argument;
        }

        internal sealed class ForLoop(Partition<Token> sourceTokens,
            string variantName,
            Expression start,
            Expression end,
            Option<Expression> step,
            IReadOnlyCollection<Statement> block)
        : BlockNode(sourceTokens, block), Statement
        {
            public string VariantName => variantName;
            public Expression Start => start;
            public Expression End => end;
            public Option<Expression> Step => step;
        }

        internal sealed class Lire(Partition<Token> sourceTokens,
            Expression argument1,
            Expression argument2)
        : NodeImpl(sourceTokens), Statement
        {
            public Expression Argument1 => argument1;
            public Expression Argument2 => argument2;
        }

        internal sealed class OuvrirAjout(Partition<Token> sourceTokens,
            Expression argument)
        : NodeImpl(sourceTokens), Statement
        {
            public Expression Argument => argument;
        }

        internal sealed class OuvrirEcriture(Partition<Token> sourceTokens,
            Expression argument)
        : NodeImpl(sourceTokens), Statement
        {
            public Expression Argument => argument;
        }

        internal sealed class OuvrirLecture(Partition<Token> sourceTokens,
            Expression argument)
        : NodeImpl(sourceTokens), Statement
        {
            public Expression Argument => argument;
        }

        internal sealed class EcrireEcran(Partition<Token> sourceTokens,
            IReadOnlyCollection<Expression> arguments)
        : NodeImpl(sourceTokens), Statement
        {
            public IReadOnlyCollection<Expression> Arguments => arguments;
        }

        internal sealed class LireClavier(Partition<Token> sourceTokens,
            Expression argument)
        : NodeImpl(sourceTokens), Statement
        {
            public Expression Argument => argument;
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

        internal sealed class VariableDeclaration(Partition<Token> sourceTokens,
            IReadOnlyCollection<string> names,
            Type type)
        : NodeImpl(sourceTokens), Statement, IEquatable<VariableDeclaration?>
        {
            public IReadOnlyCollection<string> Names => names;
            public Type Type => type;

            public override bool Equals(object? obj) => Equals(obj as VariableDeclaration);
            public bool Equals(VariableDeclaration? other) => other is not null
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
            Expression argument)
        : Expression(sourceTokens)
        {
            public Expression Argument => argument;

            public override bool Equals(Expression? other) => other is BuiltinFdf o
             && o.Argument.Equals(Argument);
            public override int GetHashCode() => Argument.GetHashCode();
        }

        internal sealed class Call(Partition<Token> sourceTokens,
            string name,
            IReadOnlyCollection<EffectiveParameter> parameters)
        : Expression(sourceTokens)
        {
            public string Name => name;
            public IReadOnlyCollection<EffectiveParameter> Parameters => parameters;

            public override bool Equals(Expression? other) => other is Call o
             && o.Name.Equals(Name)
             && o.Parameters.SequenceEqual(Parameters);
            public override int GetHashCode() => HashCode.Combine(Name, Parameters.GetSequenceHashCode());
        }

        internal sealed class ComponentAccess(Partition<Token> sourceTokens,
            Expression structure,
            string componentName)
        : Expression(sourceTokens)
        {
            public Expression Structure => structure;
            public string ComponentName => componentName;

            public override bool Equals(Expression? other) => other is ComponentAccess o
             && o.Structure.Equals(Structure)
             && o.ComponentName.Equals(ComponentName);
            public override int GetHashCode() => HashCode.Combine(Structure, ComponentName);
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

        internal sealed class ArraySubscript(Partition<Token> sourceTokens,
            Expression array,
            IReadOnlyCollection<Expression> indices)
        : Expression(sourceTokens)
        {
            public Expression Array => array;
            public IReadOnlyCollection<Expression> Indexes => indices;

            public override bool Equals(Expression? other) => other is ArraySubscript o
             && o.Array.Equals(Array)
             && o.Indexes.SequenceEqual(Indexes);
            public override int GetHashCode() => HashCode.Combine(Array, Indexes.GetSequenceHashCode());
        }

        internal sealed class VariableReference(Partition<Token> sourceTokens,
            string name)
        : Expression(sourceTokens)
        {
            public string Name => name;

            public override bool Equals(Expression? other) => other is VariableReference o
             && o.Name.Equals(Name);
            public override int GetHashCode() => Name.GetHashCode();
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

        internal sealed class String(Partition<Token> sourceTokens)
        : Type(sourceTokens)
        {
            public override bool Equals(Type? other) => true;
            public override int GetHashCode() => 0;
        }

        internal sealed class Array(Partition<Token> sourceTokens,
            Type type,
            IReadOnlyCollection<Expression> dimensions)
        : Type(sourceTokens)
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
        : Type(sourceTokens)
        {
            public PrimitiveType Type => type;
            public override bool Equals(Type? other) => other is Primitive o
             && o.Type.Equals(Type);
            public override int GetHashCode() => Type.GetHashCode();
        }

        internal sealed class AliasReference(Partition<Token> sourceTokens,
            string name)
        : Type(sourceTokens)
        {
            public string Name => name;
            public override bool Equals(Type? other) => other is AliasReference o
             && o.Name.Equals(Name);
            public override int GetHashCode() => Name.GetHashCode();
        }

        internal sealed class StringLengthed(Partition<Token> sourceTokens,
            Expression length)
        : Type(sourceTokens)
        {
            public Expression Length => length;
            public override bool Equals(Type? other) => other is StringLengthed o
             && o.Length.Equals(length);
            public override int GetHashCode() => Length.GetHashCode();
        }

        internal sealed class StructureDefinition(Partition<Token> sourceTokens,
            IReadOnlyCollection<Statement.VariableDeclaration> components)
        : Type(sourceTokens)
        {
            public IReadOnlyCollection<Statement.VariableDeclaration> Components => components;
            public override bool Equals(Type? other) => other is StructureDefinition o
             && o.Components.SequenceEqual(Components);
            public override int GetHashCode() => Components.GetSequenceHashCode();
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
        string name,
        Type type)
    : NodeImpl(sourceTokens), IEquatable<FormalParameter?>
    {
        public ParameterMode Mode => mode;
        public string Name => name;
        public Type Type => type;

        public override bool Equals(object? obj) => Equals(obj as FormalParameter);
        public bool Equals(FormalParameter? other) => other is not null
         && other.Mode.Equals(Mode)
         && other.Name.Equals(Name)
         && other.Type.Equals(Type);
        public override int GetHashCode() => HashCode.Combine(Mode, Name, Type);
    }

    internal sealed class ProcedureSignature(Partition<Token> sourceTokens,
        string name,
        IReadOnlyCollection<FormalParameter> parameters)
    : NodeImpl(sourceTokens), IEquatable<ProcedureSignature?>
    {
        public string Name => name;
        public IReadOnlyCollection<FormalParameter> Parameters => parameters;

        public override bool Equals(object? other) => Equals(other as ProcedureSignature);
        public bool Equals(ProcedureSignature? other) => other is not null
            && other.Name.Equals(Name)
            && other.Parameters.SequenceEqual(Parameters);

        public override int GetHashCode() => HashCode.Combine(Name, Parameters.GetSequenceHashCode());
    }

    internal sealed class FunctionSignature(Partition<Token> sourceTokens,
        string name,
        IReadOnlyCollection<FormalParameter> parameters,
        Type returnType)
    : NodeImpl(sourceTokens), IEquatable<FunctionSignature?>
    {
        public string Name => name;
        public IReadOnlyCollection<FormalParameter> Parameters => parameters;
        public Type ReturnType => returnType;

        public override bool Equals(object? other) => Equals(other as FunctionSignature);
        public bool Equals(FunctionSignature? other) => other is not null
            && other.Name.Equals(Name)
            && other.Parameters.SequenceEqual(Parameters)
            && other.ReturnType.Equals(ReturnType);

        public override int GetHashCode() => HashCode.Combine(Name, Parameters.GetSequenceHashCode(), ReturnType);
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
