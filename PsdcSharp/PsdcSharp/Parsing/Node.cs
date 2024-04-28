using Scover.Psdc.Tokenization;
namespace Scover.Psdc.Parsing;

#region Node traits

internal interface CallNode : Node
{
    public Identifier Name { get; }
    public IReadOnlyCollection<ParameterActual> Parameters { get; }
}

internal interface ScopedNode : Node;

internal abstract class BlockNode(Partition<Token> sourceTokens,
    IReadOnlyCollection<Node.Statement> block)
: NodeImpl(sourceTokens), ScopedNode
{
    public IReadOnlyCollection<Node.Statement> Block => block;
}

#endregion Node traits

internal interface Node
{
    bool SemanticsEqual(Node other);
    Partition<Token> SourceTokens { get; }
    internal sealed class Algorithm(Partition<Token> sourceTokens,
        Identifier name,
        IReadOnlyCollection<Declaration> declarations)
    : NodeImpl(sourceTokens), ScopedNode
    {
        public Identifier Name => name;
        public IReadOnlyCollection<Declaration> Declarations => declarations;
        public override bool SemanticsEqual(Node other) => other is Algorithm o
         && o.Name.SemanticsEqual(Name)
         && o.Declarations.AllSemanticsEqual(Declarations);
    }

    internal interface Declaration : Node
    {
        internal sealed class MainProgram(Partition<Token> sourceTokens,
            IReadOnlyCollection<Statement> block)
        : BlockNode(sourceTokens, block), Declaration
        {
            public override bool SemanticsEqual(Node other) => other is MainProgram o
             && o.Block.AllSemanticsEqual(Block);
        }

        internal sealed class TypeAlias(Partition<Token> sourceTokens,
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

        internal sealed class CompleteTypeAlias(Partition<Token> sourceTokens,
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

        internal sealed class Constant(Partition<Token> sourceTokens,
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

        internal sealed class Procedure(Partition<Token> sourceTokens,
            ProcedureSignature signature)
        : NodeImpl(sourceTokens), Declaration
        {
            public ProcedureSignature Signature => signature;
            public override bool SemanticsEqual(Node other) => other is Procedure o
             && o.Signature.SemanticsEqual(Signature);
        }

        internal sealed class ProcedureDefinition(Partition<Token> sourceTokens,
            ProcedureSignature signature,
            IReadOnlyCollection<Statement> block)
        : BlockNode(sourceTokens, block), Declaration
        {
            public ProcedureSignature Signature => signature;
            public override bool SemanticsEqual(Node other) => other is ProcedureDefinition o
             && o.Signature.SemanticsEqual(Signature)
             && o.Block.AllSemanticsEqual(Block);
        }

        internal sealed class Function(Partition<Token> sourceTokens,
            FunctionSignature signature)
        : NodeImpl(sourceTokens), Declaration
        {
            public FunctionSignature Signature => signature;
            public override bool SemanticsEqual(Node other) => other is Function o
 && o.Signature.SemanticsEqual(Signature);
        }

        internal sealed class FunctionDefinition(Partition<Token> sourceTokens,
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
        internal sealed class Nop(Partition<Token> sourceTokens)
        : NodeImpl(sourceTokens), Statement
        {
            public override bool SemanticsEqual(Node other) => other is Nop;
        }

        internal sealed class Alternative(Partition<Token> sourceTokens,
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
            internal sealed class IfClause(Partition<Token> sourceTokens,
                Expression condition,
                IReadOnlyCollection<Statement> block)
            : BlockNode(sourceTokens, block)
            {
                public Expression Condition => condition;
                public override bool SemanticsEqual(Node other) => other is IfClause o
                 && o.Condition.SemanticsEqual(Condition)
                 && o.Block.AllSemanticsEqual(Block);
            }

            internal sealed class ElseIfClause(Partition<Token> sourceTokens,
                Expression condition,
                IReadOnlyCollection<Statement> block)
            : BlockNode(sourceTokens, block)
            {
                public Expression Condition => condition;
                public override bool SemanticsEqual(Node other) => other is ElseIfClause o
                 && o.Condition.SemanticsEqual(Condition)
                 && o.Block.AllSemanticsEqual(Block);
            }

            internal sealed class ElseClause(Partition<Token> sourceTokens,
                IReadOnlyCollection<Statement> block)
            : BlockNode(sourceTokens, block)
            {
                public override bool SemanticsEqual(Node other) => other is ElseClause o
                 && o.Block.AllSemanticsEqual(Block);
            }
        }

        internal sealed class Switch(Partition<Token> sourceTokens,
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
            internal sealed class Case(Partition<Token> sourceTokens,
                Expression when,
                IReadOnlyCollection<Statement> block)
            : BlockNode(sourceTokens, block)
            {
                public Expression When => when;
                public override bool SemanticsEqual(Node other) => other is Case o
                 && o.When.SemanticsEqual(When)
                 && o.Block.AllSemanticsEqual(Block);
            }

            internal sealed class DefaultCase(Partition<Token> sourceTokens,
                IReadOnlyCollection<Statement> block)
            : BlockNode(sourceTokens, block)
            {
                public override bool SemanticsEqual(Node other) => other is DefaultCase o
                 && o.Block.AllSemanticsEqual(Block);
            }
        }

        internal sealed class Assignment(Partition<Token> sourceTokens,
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

        internal sealed class DoWhileLoop(Partition<Token> sourceTokens,
            Expression condition,
            IReadOnlyCollection<Statement> block)
        : BlockNode(sourceTokens, block), Statement
        {
            public Expression Condition => condition;
            public override bool SemanticsEqual(Node other) => other is DoWhileLoop o
             && o.Condition.SemanticsEqual(Condition)
                && o.Block.AllSemanticsEqual(Block);
        }

        internal sealed class BuiltinEcrire(Partition<Token> sourceTokens,
            Expression argumentNomLog,
            Expression argumentExpression)
        : NodeImpl(sourceTokens), Statement
        {
            public Expression ArgumentNomLog => argumentNomLog;
            public Expression ArgumentExpression => argumentExpression;
            public override bool SemanticsEqual(Node other) => other is BuiltinEcrire o
             && o.ArgumentNomLog.SemanticsEqual(ArgumentNomLog)
             && o.ArgumentExpression.SemanticsEqual(ArgumentExpression);
        }

        internal sealed class BuiltinFermer(Partition<Token> sourceTokens,
            Expression argumentNomLog)
        : NodeImpl(sourceTokens), Statement
        {
            public Expression ArgumentNomLog => argumentNomLog;
            public override bool SemanticsEqual(Node other) => other is BuiltinFermer o
             && o.ArgumentNomLog.SemanticsEqual(ArgumentNomLog);
        }

        internal sealed class ForLoop(Partition<Token> sourceTokens,
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

        internal sealed class BuiltinLire(Partition<Token> sourceTokens,
            Expression argumentNomLog,
            Expression.Lvalue argumentVariable)
        : NodeImpl(sourceTokens), Statement
        {
            public Expression ArgumentNomLog => argumentNomLog;
            public Expression ArgumentVariable => argumentVariable;
            public override bool SemanticsEqual(Node other) => other is BuiltinLire o
             && o.ArgumentNomLog.SemanticsEqual(ArgumentNomLog)
             && o.ArgumentVariable.SemanticsEqual(ArgumentVariable);
        }

        internal sealed class BuiltinOuvrirAjout(Partition<Token> sourceTokens,
            Expression argumentNomLog)
        : NodeImpl(sourceTokens), Statement
        {
            public Expression ArgumentNomLog => argumentNomLog;
            public override bool SemanticsEqual(Node other) => other is BuiltinOuvrirAjout o
             && o.ArgumentNomLog.SemanticsEqual(ArgumentNomLog);
        }

        internal sealed class BuiltinOuvrirEcriture(Partition<Token> sourceTokens,
            Expression argumentNomLog)
        : NodeImpl(sourceTokens), Statement
        {
            public Expression ArgumentNomLog => argumentNomLog;
            public override bool SemanticsEqual(Node other) => other is BuiltinOuvrirEcriture o
             && o.ArgumentNomLog.SemanticsEqual(ArgumentNomLog);
        }

        internal sealed class BuiltinOuvrirLecture(Partition<Token> sourceTokens,
            Expression argumentNomLog)
        : NodeImpl(sourceTokens), Statement
        {
            public Expression ArgumentNomLog => argumentNomLog;
            public override bool SemanticsEqual(Node other) => other is BuiltinOuvrirLecture o
             && o.ArgumentNomLog.SemanticsEqual(ArgumentNomLog);
        }

        internal sealed class ProcedureCall(Partition<Token> sourceTokens,
            Identifier name,
            IReadOnlyCollection<ParameterActual> parameters)
        : NodeImpl(sourceTokens), Statement, CallNode
        {
            public Identifier Name => name;
            public IReadOnlyCollection<ParameterActual> Parameters => parameters;
            public override bool SemanticsEqual(Node other) => other is ProcedureCall o
             && o.Name.SemanticsEqual(Name)
             && o.Parameters.AllSemanticsEqual(Parameters);
        }

        internal sealed class BuiltinAssigner(Partition<Token> sourceTokens,
            Expression.Lvalue argumentNomLog,
            Expression argumentNomExt)
        : NodeImpl(sourceTokens), Statement
        {
            public Expression.Lvalue ArgumentNomLog => argumentNomLog;
            public Expression ArgumentNomExt => argumentNomExt;
            public override bool SemanticsEqual(Node other) => other is BuiltinAssigner o
             && o.ArgumentNomLog.SemanticsEqual(ArgumentNomLog)
             && o.ArgumentNomExt.SemanticsEqual(ArgumentNomExt);
        }

        internal sealed class BuiltinEcrireEcran(Partition<Token> sourceTokens,
            IReadOnlyCollection<Expression> arguments)
        : NodeImpl(sourceTokens), Statement
        {
            public IReadOnlyCollection<Expression> Arguments => arguments;
            public override bool SemanticsEqual(Node other) => other is BuiltinEcrireEcran o
             && o.Arguments.AllSemanticsEqual(Arguments);
        }

        internal sealed class BuiltinLireClavier(Partition<Token> sourceTokens,
            Expression.Lvalue argumentVariable)
        : NodeImpl(sourceTokens), Statement
        {
            public Expression ArgumentVariable => argumentVariable;
            public override bool SemanticsEqual(Node other) => other is BuiltinLireClavier o
             && o.ArgumentVariable.SemanticsEqual(ArgumentVariable);
        }

        internal sealed class RepeatLoop(Partition<Token> sourceTokens,
            Expression condition,
            IReadOnlyCollection<Statement> block)
        : BlockNode(sourceTokens, block), Statement
        {
            public Expression Condition => condition;
            public override bool SemanticsEqual(Node other) => other is RepeatLoop o
             && o.Condition.SemanticsEqual(Condition)
             && o.Block.AllSemanticsEqual(Block);
        }

        internal sealed class Return(Partition<Token> sourceTokens,
            Expression value)
        : NodeImpl(sourceTokens), Statement
        {
            public Expression Value => value;
            public override bool SemanticsEqual(Node other) => other is Return o
             && o.Value.SemanticsEqual(Value);
        }

        internal sealed class LocalVariable(Partition<Token> sourceTokens,
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

        internal sealed class WhileLoop(Partition<Token> sourceTokens,
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
            internal sealed class ComponentAccess(Partition<Token> sourceTokens,
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

            internal sealed new class Bracketed(Partition<Token> sourceTokens,
                Lvalue lvalue)
            : NodeImpl(sourceTokens), Lvalue
            {
                public Lvalue Lvalue => lvalue;
                public override bool SemanticsEqual(Node other) => other is Bracketed o
                 && o.Lvalue.SemanticsEqual(Lvalue);
            }

            internal sealed class ArraySubscript(Partition<Token> sourceTokens,
                Expression array,
                IReadOnlyCollection<Expression> indices)
            : NodeImpl(sourceTokens), Lvalue
            {
                public Expression Array => array;
                public IReadOnlyCollection<Expression> Indexes => indices;
                public override bool SemanticsEqual(Node other) => other is ArraySubscript o
                 && o.Array.SemanticsEqual(Array)
                 && o.Indexes.AllSemanticsEqual(Indexes);
            }

            internal sealed class VariableReference(Partition<Token> sourceTokens,
                Identifier name)
            : NodeImpl(sourceTokens), Lvalue
            {
                public Identifier Name => name;
                public override bool SemanticsEqual(Node other) => other is VariableReference o
                 && o.Name.SemanticsEqual(Name);
            }
        }

        internal sealed class OperationUnary(Partition<Token> sourceTokens,
            UnaryOperator @operator,
            Expression operand)
        : NodeImpl(sourceTokens), Expression
        {
            public UnaryOperator Operator => @operator;
            public Expression Operand => operand;
            public override bool SemanticsEqual(Node other) => other is OperationUnary o
             && o.Operator.Equals(Operator)
             && o.Operand.SemanticsEqual(Operand);
        }

        internal sealed class OperationBinary(Partition<Token> sourceTokens,
            Expression operand1,
            BinaryOperator @operator,
            Expression operand2)
        : NodeImpl(sourceTokens), Expression
        {
            public Expression Operand1 => operand1;
            public BinaryOperator Operator => @operator;
            public Expression Operand2 => operand2;
            public override bool SemanticsEqual(Node other) => other is OperationBinary o
             && o.Operand1.SemanticsEqual(Operand1)
             && o.Operator.Equals(Operator)
             && o.Operand2.SemanticsEqual(Operand2);
        }

        internal sealed class BuiltinFdf(Partition<Token> sourceTokens,
            Expression argumentNomLog)
        : NodeImpl(sourceTokens), Expression
        {
            public Expression ArgumentNomLog => argumentNomLog;
            public override bool SemanticsEqual(Node other) => other is BuiltinFdf o
             && o.ArgumentNomLog.SemanticsEqual(ArgumentNomLog);
        }

        internal sealed class FunctionCall(Partition<Token> sourceTokens,
            Identifier name,
            IReadOnlyCollection<ParameterActual> parameters)
        : NodeImpl(sourceTokens), Expression, CallNode
        {
            public Identifier Name => name;
            public IReadOnlyCollection<ParameterActual> Parameters => parameters;
            public override bool SemanticsEqual(Node other) => other is FunctionCall o
             && o.Name.SemanticsEqual(Name)
             && o.Parameters.AllSemanticsEqual(Parameters);
        }

        internal sealed class Bracketed(Partition<Token> sourceTokens,
            Expression expression)
        : NodeImpl(sourceTokens), Expression
        {
            public Expression Expression => expression;
            public override bool SemanticsEqual(Node other) => other is Bracketed o
             && o.Expression.SemanticsEqual(Expression);
        }

        internal sealed class True(Partition<Token> sourceTokens)
        : NodeImpl(sourceTokens), Expression
        {
            public override bool SemanticsEqual(Node other) => other is True;
        }

        internal sealed class False(Partition<Token> sourceTokens)
        : NodeImpl(sourceTokens), Expression
        {
            public override bool SemanticsEqual(Node other) => other is False;
        }

        internal interface Literal : Expression
        {
            public string Value { get; }
            internal sealed class Character(Partition<Token> sourceTokens,
                string value)
            : NodeImpl(sourceTokens), Literal
            {
                public string Value => value;
                public override bool SemanticsEqual(Node other) => other is Character o
                 && o.Value.Equals(Value);
            }

            internal sealed class Integer(Partition<Token> sourceTokens,
                string value)
            : NodeImpl(sourceTokens), Literal
            {
                public string Value => value;
                public override bool SemanticsEqual(Node other) => other is Integer o
                 && o.Value.Equals(Value);
            }

            internal sealed class Real(Partition<Token> sourceTokens,
                string value)
            : NodeImpl(sourceTokens), Literal
            {
                public string Value => value;
                public override bool SemanticsEqual(Node other) => other is Real o
                 && o.Value.Equals(Value);
            }

            internal sealed class String(Partition<Token> sourceTokens,
                string value)
            : NodeImpl(sourceTokens), Literal
            {
                public string Value => value;
                public override bool SemanticsEqual(Node other) => other is String o
                 && o.Value.Equals(Value);
            }
        }
    }

    internal interface Type : Node
    {
        internal sealed class AliasReference(Partition<Token> sourceTokens,
            Identifier name)
        : NodeImpl(sourceTokens), Type
        {
            public Identifier Name => name;
            public override bool SemanticsEqual(Node other) => other is AliasReference o
             && o.Name.SemanticsEqual(Name);
        }

        internal sealed class String(Partition<Token> sourceTokens)
        : NodeImpl(sourceTokens), Type
        {
            public override bool SemanticsEqual(Node other) => other is String;
        }

        internal interface Complete : Type
        {
            internal sealed class Array(Partition<Token> sourceTokens,
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

            internal sealed class Primitive(Partition<Token> sourceTokens,
                PrimitiveType type)
            : NodeImpl(sourceTokens), Complete
            {
                public PrimitiveType Type => type;
                public override bool SemanticsEqual(Node other) => other is Primitive o
                 && o.Type.Equals(Type);
            }

            internal sealed new class AliasReference(Partition<Token> sourceTokens,
                Identifier name)
            : NodeImpl(sourceTokens), Complete
            {
                public Identifier Name => name;
                public override bool SemanticsEqual(Node other) => other is AliasReference o
                 && o.Name.SemanticsEqual(Name);
            }

            internal sealed class LengthedString(Partition<Token> sourceTokens,
                Expression length)
            : NodeImpl(sourceTokens), Complete
            {
                public Expression Length => length;
                public override bool SemanticsEqual(Node other) => other is LengthedString o
                 && o.Length.SemanticsEqual(Length);
            }

            internal sealed class Structure(Partition<Token> sourceTokens,
                IReadOnlyCollection<Statement.LocalVariable> components)
            : NodeImpl(sourceTokens), Complete
            {
                public IReadOnlyCollection<Statement.LocalVariable> Components => components;
                public override bool SemanticsEqual(Node other) => other is Structure o
                 && o.Components.AllSemanticsEqual(Components);
            }
        }
    }

    internal sealed class ParameterActual(Partition<Token> sourceTokens,
        ParameterMode mode,
        Expression value)
    : NodeImpl(sourceTokens)
    {
        public ParameterMode Mode => mode;
        public Expression Value => value;
        public override bool SemanticsEqual(Node other) => other is ParameterActual o
         && o.Mode.Equals(Mode)
         && o.Value.SemanticsEqual(Value);
    }

    internal sealed class ParameterFormal(Partition<Token> sourceTokens,
        ParameterMode mode,
        Identifier name,
        Type type)
    : NodeImpl(sourceTokens)
    {
        public ParameterMode Mode => mode;
        public Identifier Name => name;
        public Type Type => type;
        public override bool SemanticsEqual(Node other) => other is ParameterFormal o
         && o.Mode.Equals(Mode)
         && o.Name.SemanticsEqual(Name)
         && o.Type.SemanticsEqual(Type);
    }

    internal sealed class ProcedureSignature(Partition<Token> sourceTokens,
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

    internal sealed class FunctionSignature(Partition<Token> sourceTokens,
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

internal sealed class Identifier : NodeImpl, IEquatable<Identifier?>
{
    public Identifier(Partition<Token> sourceTokens, string name) : base(sourceTokens)
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
    public override string ToString() => Name;

    public override bool SemanticsEqual(Node other) => other is Identifier o && o.Name.Equals(Name);
    public override bool Equals(object? obj) => Equals(obj as Identifier);
    public bool Equals(Identifier? other) => other is not null && other.Name == Name;
    public override int GetHashCode() => Name.GetHashCode();
}

#endregion Terminals

internal abstract class NodeImpl(Partition<Token> sourceTokens) : Node
{
    public Partition<Token> SourceTokens => sourceTokens;
    public abstract bool SemanticsEqual(Node other);
}
