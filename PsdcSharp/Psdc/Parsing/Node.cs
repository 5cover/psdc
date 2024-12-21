global using Block = System.Collections.Generic.IReadOnlyList<Scover.Psdc.Parsing.Node.Stmt>;
using Scover.Psdc.Pseudocode;

namespace Scover.Psdc.Parsing;

public interface Node : EquatableSemantics<Node>
{
    Range Location { get; }

    internal interface ParenExpr : Expr
    {
        public Expr InnerExpr { get; }
    }

    public interface CompilerDirective : Declaration, Stmt, Component, Initializer.Braced.Item
    {
        internal sealed record EvalExpr(Range Location, Expr Expr) : CompilerDirective
        {
            public bool SemanticsEqual(Node other) => other is EvalExpr o
                && o.Expr.SemanticsEqual(Expr);
        }

        internal sealed record EvalType(Range Location, Type Type) : CompilerDirective
        {
            public bool SemanticsEqual(Node other) => other is EvalType o
                && o.Type.SemanticsEqual(Type);
        }

        internal sealed record Assert(Range Location, Expr Expr, Option<Expr> Message) : CompilerDirective
        {
            public bool SemanticsEqual(Node other) => other is Assert o
                && o.Expr.SemanticsEqual(Expr)
                && o.Message.OptionSemanticsEqual(Message);
        }
    }

    public sealed record Algorithm(Range Location,
        IReadOnlyList<CompilerDirective> LeadingDirectives,
        Identifier Title,
        IReadOnlyList<Declaration> Declarations) : Node
    {
        public bool SemanticsEqual(Node other) => other is Algorithm o
         && o.LeadingDirectives.AllSemanticsEqual(LeadingDirectives)
         && o.Title.SemanticsEqual(Title)
         && o.Declarations.AllSemanticsEqual(Declarations);
    }

    public interface Declaration : Node
    {
        internal sealed record MainProgram(Range Location,
            Block Block)
        : Declaration
        {
            public bool SemanticsEqual(Node other) => other is MainProgram o
             && o.Block.AllSemanticsEqual(Block);
        }

        internal sealed record TypeAlias(Range Location,
            Identifier Name,
            Type Type)
        : Declaration
        {
            public bool SemanticsEqual(Node other) => other is TypeAlias o
             && o.Name.SemanticsEqual(Name)
             && o.Type.SemanticsEqual(Type);
        }

        internal sealed record Constant(Range Location,
            Type Type,
            Identifier Name,
            Initializer Value)
        : Declaration
        {
            public bool SemanticsEqual(Node other) => other is Constant o
             && o.Name.SemanticsEqual(Name)
             && o.Type.SemanticsEqual(Type)
             && o.Value.SemanticsEqual(Value);
        }

        internal sealed record Procedure(Range Location,
            ProcedureSignature Signature)
        : Declaration
        {
            public bool SemanticsEqual(Node other) => other is Procedure o
             && o.Signature.SemanticsEqual(Signature);
        }

        internal sealed record ProcedureDefinition(Range Location,
            ProcedureSignature Signature,
            Block Block)
        : Declaration
        {
            public bool SemanticsEqual(Node other) => other is ProcedureDefinition o
             && o.Signature.SemanticsEqual(Signature)
             && o.Block.AllSemanticsEqual(Block);
        }

        internal sealed record Function(Range Location,
            FunctionSignature Signature)
        : Declaration
        {
            public bool SemanticsEqual(Node other) => other is Function o
             && o.Signature.SemanticsEqual(Signature);
        }

        internal sealed record FunctionDefinition(Range Location,
            FunctionSignature Signature,
            Block Block)
        : Declaration
        {
            public bool SemanticsEqual(Node other) => other is FunctionDefinition o
             && o.Signature.SemanticsEqual(Signature)
             && o.Block.AllSemanticsEqual(Block);
        }
    }

    public interface Stmt : Node
    {
        internal sealed record ExprStmt(Range Location,
            Expr Expr) : Stmt
        {
            public bool SemanticsEqual(Node other) => other is ExprStmt o
             && o.Expr.SemanticsEqual(Expr);
        }
        internal sealed record Alternative(Range Location,
            Alternative.IfClause If,
            IReadOnlyList<Alternative.ElseIfClause> ElseIfs,
            Option<Alternative.ElseClause> Else)
        : Stmt
        {

            public bool SemanticsEqual(Node other) => other is Alternative o
             && o.If.SemanticsEqual(If)
             && o.ElseIfs.AllSemanticsEqual(ElseIfs)
             && o.Else.OptionSemanticsEqual(Else);

            internal sealed record IfClause(Range Location,
                Expr Condition,
                Block Block)
            : Node
            {
                public bool SemanticsEqual(Node other) => other is IfClause o
                 && o.Condition.SemanticsEqual(Condition)
                 && o.Block.AllSemanticsEqual(Block);
            }

            internal sealed record ElseIfClause(Range Location,
                Expr Condition,
                Block Block)
            : Node
            {
                public bool SemanticsEqual(Node other) => other is ElseIfClause o
                 && o.Condition.SemanticsEqual(Condition)
                 && o.Block.AllSemanticsEqual(Block);
            }

            internal sealed record ElseClause(Range Location,
                Block Block)
            : Node
            {
                public bool SemanticsEqual(Node other) => other is ElseClause o
                 && o.Block.AllSemanticsEqual(Block);
            }
        }

        internal sealed record Switch(Range Location,
            Expr Expr,
            IReadOnlyList<Switch.Case> Cases)
        : Stmt
        {
            public bool SemanticsEqual(Node other) => other is Switch o
                 && o.Expr.SemanticsEqual(Expr)
                 && o.Cases.AllSemanticsEqual(Cases);

            internal interface Case : Node
            {
                public Block Block { get; }
                internal sealed record OfValue(Range Location,
                    Expr Value,
                    Block Block)
                : Case
                {
                    public bool SemanticsEqual(Node other) => other is OfValue o
                     && o.Value.SemanticsEqual(Value)
                     && o.Block.AllSemanticsEqual(Block);
                }

                internal sealed record Default(Range Location,
                    Block Block)
                : Case
                {
                    public bool SemanticsEqual(Node other) => other is Default o
                     && o.Block.AllSemanticsEqual(Block);
                }
            }
        }

        internal sealed record Assignment(Range Location,
            Expr.Lvalue Target,
            Expr Value)
        : Stmt
        {
            public bool SemanticsEqual(Node other) => other is Assignment o
             && o.Target.SemanticsEqual(Target)
             && o.Value.SemanticsEqual(Value);
        }

        internal sealed record DoWhileLoop(Range Location,
            Expr Condition,
            Block Block)
        : Node, Stmt
        {
            public bool SemanticsEqual(Node other) => other is DoWhileLoop o
             && o.Condition.SemanticsEqual(Condition)
                && o.Block.AllSemanticsEqual(Block);
        }

        internal interface Builtin : Stmt
        {
            internal sealed record Ecrire(Range Location,
                Expr ArgumentNomLog,
                Expr ArgumentExpression)
            : Builtin
            {
                public bool SemanticsEqual(Node other) => other is Ecrire o
                 && o.ArgumentNomLog.SemanticsEqual(ArgumentNomLog)
                 && o.ArgumentExpression.SemanticsEqual(ArgumentExpression);
            }

            internal sealed record Fermer(Range Location,
                Expr ArgumentNomLog)
            : Builtin
            {
                public bool SemanticsEqual(Node other) => other is Fermer o
                 && o.ArgumentNomLog.SemanticsEqual(ArgumentNomLog);
            }

            internal sealed record Lire(Range Location,
                Expr ArgumentNomLog,
                Expr.Lvalue ArgumentVariable)
            : Builtin
            {
                public bool SemanticsEqual(Node other) => other is Lire o
                 && o.ArgumentNomLog.SemanticsEqual(ArgumentNomLog)
                 && o.ArgumentVariable.SemanticsEqual(ArgumentVariable);
            }

            internal sealed record OuvrirAjout(Range Location,
                Expr ArgumentNomLog)
            : Builtin
            {
                public bool SemanticsEqual(Node other) => other is OuvrirAjout o
                 && o.ArgumentNomLog.SemanticsEqual(ArgumentNomLog);
            }

            internal sealed record OuvrirEcriture(Range Location,
                Expr ArgumentNomLog)
            : Builtin
            {
                public bool SemanticsEqual(Node other) => other is OuvrirEcriture o
                 && o.ArgumentNomLog.SemanticsEqual(ArgumentNomLog);
            }

            internal sealed record OuvrirLecture(Range Location,
                Expr ArgumentNomLog)
            : Builtin
            {
                public bool SemanticsEqual(Node other) => other is OuvrirLecture o
                 && o.ArgumentNomLog.SemanticsEqual(ArgumentNomLog);
            }

            internal sealed record Assigner(Range Location,
                Expr.Lvalue ArgumentNomLog,
                Expr ArgumentNomExt)
            : Builtin
            {
                public bool SemanticsEqual(Node other) => other is Assigner o
                 && o.ArgumentNomLog.SemanticsEqual(ArgumentNomLog)
                 && o.ArgumentNomExt.SemanticsEqual(ArgumentNomExt);
            }

            internal sealed record EcrireEcran(Range Location,
                IReadOnlyList<Expr> Arguments)
            : Builtin
            {
                public bool SemanticsEqual(Node other) => other is EcrireEcran o
                 && o.Arguments.AllSemanticsEqual(Arguments);
            }

            internal sealed record LireClavier(Range Location,
                Expr.Lvalue ArgumentVariable)
            : Builtin
            {
                public bool SemanticsEqual(Node other) => other is LireClavier o
                 && o.ArgumentVariable.SemanticsEqual(ArgumentVariable);
            }
        }

        internal sealed record ForLoop(Range Location,
            Expr.Lvalue Variant,
            Expr Start,
            Expr End,
            Option<Expr> Step,
            Block Block)
        : Stmt
        {
            public bool SemanticsEqual(Node other) => other is ForLoop o
             && o.Variant.SemanticsEqual(Variant)
             && o.Start.SemanticsEqual(Start)
             && o.End.SemanticsEqual(End)
             && o.Step.OptionSemanticsEqual(Step)
             && o.Block.AllSemanticsEqual(Block);
        }

        internal sealed record RepeatLoop(Range Location,
            Expr Condition,
            Block Block)
        : Node, Stmt
        {
            public bool SemanticsEqual(Node other) => other is RepeatLoop o
             && o.Condition.SemanticsEqual(Condition)
             && o.Block.AllSemanticsEqual(Block);
        }

        internal sealed record Return(Range Location,
            Option<Expr> Value)
        : Stmt
        {
            public bool SemanticsEqual(Node other) => other is Return o
             && o.Value.OptionSemanticsEqual(Value);
        }

        internal sealed record LocalVariable(Range Location,
            VariableDeclaration Declaration,
            Option<Initializer> Value)
        : Stmt
        {
            public bool SemanticsEqual(Node other) => other is LocalVariable o
             && o.Declaration.SemanticsEqual(Declaration)
             && o.Value.OptionSemanticsEqual(Value);
        }

        internal sealed record WhileLoop(Range Location,
            Expr Condition,
            Block Block)
        : Node, Stmt
        {
            public bool SemanticsEqual(Node other) => other is WhileLoop o
             && o.Condition.SemanticsEqual(Condition)
             && o.Block.AllSemanticsEqual(Block);
        }
    }

    public interface Initializer : Node
    {
        public sealed record Braced(Range Location,
            IReadOnlyList<Braced.Item> Items)
        : Initializer
        {
            public bool SemanticsEqual(Node other) => other is Braced o
             && o.Items.AllSemanticsEqual(Items);

            public interface Item : Node;

            internal sealed record ValuedItem(Range Location,
                IReadOnlyList<Designator> Designators,
                Initializer Value)
            : Item
            {
                public bool SemanticsEqual(Node other) => other is ValuedItem o
                 && o.Designators.AllSemanticsEqual(Designators)
                 && o.Value.SemanticsEqual(Value);
            }
        }
    }

    internal interface Expr : Initializer
    {
        internal interface Lvalue : Expr
        {
            internal sealed record ComponentAccess(Range Location,
                Expr Structure,
                Identifier ComponentName)
            : Lvalue
            {
                public bool SemanticsEqual(Node other) => other is ComponentAccess o
                 && o.Structure.SemanticsEqual(Structure)
                 && o.ComponentName.SemanticsEqual(ComponentName);
            }

            internal sealed record ParenLValue
            : Lvalue, ParenExpr
            {
                public ParenLValue(Range location,
                Lvalue lvalue) => (Location, ContainedLvalue) = (location,
                    lvalue is ParenExpr b
                    && b.InnerExpr is Lvalue l
                    ? l : lvalue);
                public Range Location { get; }
                public Lvalue ContainedLvalue { get; }
                Expr ParenExpr.InnerExpr => ContainedLvalue;

                public bool SemanticsEqual(Node other) => other is ParenLValue o
                 && o.ContainedLvalue.SemanticsEqual(ContainedLvalue);
            }

            internal sealed record ArraySubscript(Range Location,
                Expr Array,
                Expr Index)
            : Lvalue
            {
                public bool SemanticsEqual(Node other) => other is ArraySubscript o
                 && o.Array.SemanticsEqual(Array)
                 && o.Index.SemanticsEqual(Index);
            }

            internal sealed record VariableReference(Range Location,
                Identifier Name)
            : Lvalue
            {
                public bool SemanticsEqual(Node other) => other is VariableReference o
                 && o.Name.SemanticsEqual(Name);
            }
        }

        internal sealed record UnaryOperation(Range Location,
            UnaryOperator Operator,
            Expr Operand)
        : Expr
        {
            public bool SemanticsEqual(Node other) => other is UnaryOperation o
             && o.Operator.Equals(Operator)
             && o.Operand.SemanticsEqual(Operand);
        }

        internal sealed record BinaryOperation(Range Location,
            Expr Left,
            BinaryOperator Operator,
            Expr Right)
        : Expr
        {
            public bool SemanticsEqual(Node other) => other is BinaryOperation o
             && o.Left.SemanticsEqual(Left)
             && o.Operator.Equals(Operator)
             && o.Right.SemanticsEqual(Right);
        }

        internal sealed record BuiltinFdf(Range Location,
            Expr ArgumentNomLog)
        : Expr
        {
            public bool SemanticsEqual(Node other) => other is BuiltinFdf o
             && o.ArgumentNomLog.SemanticsEqual(ArgumentNomLog);
        }

        internal sealed record Call(Range Location,
            Identifier Callee,
            IReadOnlyList<ParameterActual> Parameters)
        : Expr
        {
            public bool SemanticsEqual(Node other) => other is Call o
             && o.Callee.SemanticsEqual(Callee)
             && o.Parameters.AllSemanticsEqual(Parameters);
        }

        internal sealed record ParenExprImpl
        : ParenExpr
        {
            public ParenExprImpl(Range location,
            Expr expr) => (Location, InnerExpr) = (location,
                expr is ParenExpr b ? b.InnerExpr : expr);
            public Range Location { get; }
            public Expr InnerExpr { get; }

            public bool SemanticsEqual(Node other) => other is ParenExprImpl o
             && o.InnerExpr.SemanticsEqual(InnerExpr);
        }

        internal abstract record Literal<TType, TValue, TUnderlying>(Range Location,
            TType ValueType,
            TUnderlying Value)
        : Literal
            where TValue : Value
            where TType : EvaluatedType, InstantiableType<TValue, TUnderlying>
            where TUnderlying : notnull
        {
            object Literal.Value => Value;
            EvaluatedType Literal.ValueType => ValueType;

            public Value CreateValue() => ValueType.Instanciate(Value);

            public bool SemanticsEqual(Node other) => other is Literal<TType, TValue, TUnderlying> o
             && o.Value.Equals(Value);
        }

        internal interface Literal : Expr
        {
            object Value { get; }
            EvaluatedType ValueType { get; }
            Value CreateValue();

            internal sealed record True(Range Location)
            : Literal<BooleanType, BooleanValue, bool>(Location, BooleanType.Instance, true);

            internal sealed record False(Range Location)
            : Literal<BooleanType, BooleanValue, bool>(Location, BooleanType.Instance, false);

            internal sealed record Character(Range Location, char Value)
            : Literal<CharacterType, CharacterValue, char>(Location, CharacterType.Instance, Value);

            internal sealed record Integer(Range Location, int Value)
            : Literal<IntegerType, IntegerValue, int>(Location, IntegerType.Instance, Value)
            {
                public Integer(Range location, string valueStr)
                : this(location, int.Parse(valueStr, Format.Code)) { }
            }

            internal sealed record Real(Range Location, decimal Value)
            : Literal<RealType, RealValue, decimal>(Location, RealType.Instance, Value)
            {
                public Real(Range location, string valueStr)
                : this(location, decimal.Parse(valueStr, Format.Code)) { }
            }

            internal sealed record String(Range Location, string Value)
            : Literal<LengthedStringType, LengthedStringValue, string>(Location, LengthedStringType.Create(Value.Length), Value);
        }
    }

    internal interface Type : Node
    {
        internal sealed record AliasReference(Range Location,
            Identifier Name)
        : Type
        {
            public bool SemanticsEqual(Node other) => other is AliasReference o
             && o.Name.SemanticsEqual(Name);
        }

        internal sealed record String(Range Location)
        : Type
        {
            public bool SemanticsEqual(Node other) => other is String;
        }

        internal sealed record Array(Range Location,
            Type Type,
            IReadOnlyList<Expr> Dimensions)
        : Type
        {
            public bool SemanticsEqual(Node other) => other is Array o
             && o.Type.SemanticsEqual(Type)
             && o.Dimensions.AllSemanticsEqual(Dimensions);
        }

        internal sealed record File(Range Location)
        : Type
        {
            public bool SemanticsEqual(Node other) => other is File;
        }

        internal sealed record Character(Range Location)
        : Type
        {
            public bool SemanticsEqual(Node other) => other is Character;
        }

        internal sealed record Boolean(Range Location)
        : Type
        {
            public bool SemanticsEqual(Node other) => other is Boolean;
        }

        internal sealed record Integer(Range Location)
        : Type
        {
            public bool SemanticsEqual(Node other) => other is Integer;
        }

        internal sealed record Real(Range Location)
        : Type
        {
            public bool SemanticsEqual(Node other) => other is Real;
        }

        internal sealed record LengthedString(Range Location,
            Expr Length)
        : Type
        {
            public bool SemanticsEqual(Node other) => other is LengthedString o
             && o.Length.SemanticsEqual(Length);
        }

        internal sealed record Structure(Range Location,
            IReadOnlyList<Component> Components)
        : Type
        {
            public bool SemanticsEqual(Node other) => other is Structure o
             && o.Components.AllSemanticsEqual(Components);
        }
    }

    internal interface Designator : Node
    {
        internal sealed record Array(Range Location,
            Expr Index)
        : Designator
        {
            public bool SemanticsEqual(Node other) => other is Array o
             && o.Index.SemanticsEqual(Index);
        }

        internal sealed record Structure(Range Location,
            Identifier Component)
        : Designator
        {
            public bool SemanticsEqual(Node other) => other is Structure o
             && o.Component.SemanticsEqual(Component);
        }
    }

    public interface Component : Node;

    internal sealed record Nop(Range Location) : Stmt, Declaration
    {
        public bool SemanticsEqual(Node other) => other is Nop;
    }

    internal sealed record ParameterActual(Range Location,
        ParameterMode Mode,
        Expr Value)
    : Node
    {
        public bool SemanticsEqual(Node other) => other is ParameterActual o
         && o.Mode == Mode
         && o.Value.SemanticsEqual(Value);
    }

    internal sealed record ParameterFormal(Range Location,
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

    internal sealed record VariableDeclaration(Range Location,
        IReadOnlyList<Identifier> Names,
        Type Type)
    : Component
    {
        public bool SemanticsEqual(Node other) => other is VariableDeclaration o
         && o.Names.AllSemanticsEqual(Names)
         && o.Type.SemanticsEqual(Type);
    }

    internal sealed record ProcedureSignature(Range Location,
        Identifier Name,
        IReadOnlyList<ParameterFormal> Parameters)
    : Node
    {
        public bool SemanticsEqual(Node other) => other is ProcedureSignature o
         && o.Name.SemanticsEqual(Name)
         && o.Parameters.AllSemanticsEqual(Parameters);
    }

    internal sealed record FunctionSignature(Range Location,
        Identifier Name,
        IReadOnlyList<ParameterFormal> Parameters,
        Type ReturnType)
    : Node
    {
        public bool SemanticsEqual(Node other) => other is FunctionSignature o
         && o.Name.SemanticsEqual(Name)
         && o.Parameters.AllSemanticsEqual(Parameters)
         && o.ReturnType.SemanticsEqual(ReturnType);
    }

    internal abstract record UnaryOperator(Range Location, string Representation) : Node
    {
        public bool SemanticsEqual(Node other) => other is UnaryOperator o
         && o.Representation == Representation;

        public sealed record Cast(Range Location, Type Target) : UnaryOperator(Location, "(cast)");
        public sealed record Minus(Range Location) : UnaryOperator(Location, "-");
        public sealed record Not(Range Location) : UnaryOperator(Location, "NON");
        public sealed record Plus(Range Location) : UnaryOperator(Location, "+");
    }

    internal abstract record BinaryOperator(Range Location, string Representation) : Node
    {
        public bool SemanticsEqual(Node other) => other is UnaryOperator o
         && o.Representation == Representation;

        public sealed record Add(Range Location) : BinaryOperator(Location, "ET");
        public sealed record And(Range Location) : BinaryOperator(Location, "/");
        public sealed record Divide(Range Location) : BinaryOperator(Location, "==");
        public sealed record Equal(Range Location) : BinaryOperator(Location, ">");
        public sealed record GreaterThan(Range Location) : BinaryOperator(Location, ">=");
        public sealed record GreaterThanOrEqual(Range Location) : BinaryOperator(Location, "<");
        public sealed record LessThan(Range Location) : BinaryOperator(Location, "<=");
        public sealed record LessThanOrEqual(Range Location) : BinaryOperator(Location, "-");
        public sealed record Mod(Range Location) : BinaryOperator(Location, "%");
        public sealed record Multiply(Range Location) : BinaryOperator(Location, "*");
        public sealed record NotEqual(Range Location) : BinaryOperator(Location, "!=");
        public sealed record Or(Range Location) : BinaryOperator(Location, "OU");
        public sealed record Subtract(Range Location) : BinaryOperator(Location, "+");
        public sealed record Xor(Range Location) : BinaryOperator(Location, "XOR");
    }
}
