using Scover.Psdc.Pseudocode;

namespace Scover.Psdc.Parsing;

public interface Node
{
    Range Location { get; }

    internal interface ParenExpr : Expr
    {
        Expr InnerExpr { get; }
    }
    sealed record Algorithm(Range Location,
        IReadOnlyList<CompilerDirective> LeadingDirectives,
        Ident Title,
        IReadOnlyList<Declaration> Declarations) : Node;
    interface Declaration : Node
    {
        internal sealed record MainProgram(Range Location, IReadOnlyList<Stmt> Body) : Declaration;
        internal sealed record TypeAlias(Range Location, Ident Name, Type Type) : Declaration;
        internal sealed record Constant(Range Location, Type Type, Ident Name, Initializer Value) : Declaration;
        internal sealed record Procedure(Range Location, ProcedureSignature Signature) : Declaration;
        internal sealed record ProcedureDefinition(Range Location, ProcedureSignature Signature, IReadOnlyList<Stmt> Body) : Declaration;
        internal sealed record Function(Range Location, FunctionSignature Signature) : Declaration;
        internal sealed record FunctionDefinition(Range Location, FunctionSignature Signature, IReadOnlyList<Stmt> Body) : Declaration;
    }
    interface Stmt : Node
    {
        internal sealed record ExprStmt(Range Location, Expr Expr) : Stmt;
        internal sealed record Assignment(Range Location, Expr.Lvalue Target, Expr Value) : Stmt;
        internal sealed record DoWhileLoop(Range Location, Expr Condition, IReadOnlyList<Stmt> Body) : Stmt;

        internal sealed record Alternative(Range Location,
            Alternative.IfClause If,
            IReadOnlyList<Alternative.ElseIfClause> ElseIfs,
            Option<Alternative.ElseClause> Else)
        : Stmt
        {
            internal sealed record IfClause(Range Location, Expr Condition, IReadOnlyList<Stmt> Body) : Node;
            internal sealed record ElseIfClause(Range Location, Expr Condition, IReadOnlyList<Stmt> Body) : Node;
            internal sealed record ElseClause(Range Location, IReadOnlyList<Stmt> Body) : Node;
        }

        internal sealed record Switch(Range Location, Expr Expr, IReadOnlyList<Switch.Case> Cases) : Stmt
        {
            internal interface Case : Node
            {
                IReadOnlyList<Stmt> Body { get; }
                internal sealed record OfValue(Range Location, Expr Value, IReadOnlyList<Stmt> Body) : Case;
                internal sealed record Default(Range Location, IReadOnlyList<Stmt> Body) : Case;
            }
        }

        internal interface Builtin : Stmt
        {
            internal sealed record Ecrire(Range Location, Expr ArgumentNomLog, Expr ArgumentExpression) : Builtin;
            internal sealed record Fermer(Range Location, Expr ArgumentNomLog) : Builtin;
            internal sealed record Lire(Range Location, Expr ArgumentNomLog, Expr.Lvalue ArgumentVariable) : Builtin;
            internal sealed record OuvrirAjout(Range Location, Expr ArgumentNomLog) : Builtin;
            internal sealed record OuvrirEcriture(Range Location, Expr ArgumentNomLog) : Builtin;
            internal sealed record OuvrirLecture(Range Location, Expr ArgumentNomLog) : Builtin;
            internal sealed record Assigner(Range Location, Expr.Lvalue ArgumentNomLog, Expr ArgumentNomExt) : Builtin;
            internal sealed record EcrireEcran(Range Location, IReadOnlyList<Expr> Arguments) : Builtin;
            internal sealed record LireClavier(Range Location, Expr.Lvalue ArgumentVariable) : Builtin;
        }

        internal sealed record ForLoop(Range Location,
            Expr.Lvalue Variant,
            Expr Start,
            Expr End,
            Option<Expr> Step,
            IReadOnlyList<Stmt> Body)
        : Stmt;

        internal sealed record RepeatLoop(Range Location,
            Expr Condition,
            IReadOnlyList<Stmt> Body)
        : Stmt;

        internal sealed record Return(Range Location,
            Option<Expr> Value)
        : Stmt;

        internal sealed record LocalVariable(Range Location,
            VariableDeclaration Decl,
            Option<Initializer> Value)
        : Stmt;

        internal sealed record WhileLoop(Range Location,
            Expr Condition,
            IReadOnlyList<Stmt> Body)
        : Stmt;
    }
    interface Initializer : Node
    {
        sealed record Braced(Range Location, IReadOnlyList<Braced.Item> Items) : Initializer
        {
            public interface Item : Node;
            internal sealed record ValuedItem(Range Location, IReadOnlyList<Designator> Designators, Initializer Value) : Item;
        }
    }

    internal interface Expr : Initializer
    {
        internal interface Lvalue : Expr
        {
            internal sealed record ComponentAccess(Range Location,
                Expr Structure,
                Ident ComponentName)
            : Lvalue;

            internal sealed record ParenLValue
            : Lvalue, ParenExpr
            {
                public ParenLValue(Range location,
                Lvalue lvalue) => (Location, ContainedLvalue) = (location,
                    lvalue is ParenExpr { InnerExpr: Lvalue l }
                        ? l : lvalue);
                public Range Location { get; }
                public Lvalue ContainedLvalue { get; }
                Expr ParenExpr.InnerExpr => ContainedLvalue;

            }

            internal sealed record ArraySubscript(Range Location,
                Expr Array,
                Expr Index)
            : Lvalue;

            internal sealed record VariableReference(Range Location,
                Ident Name)
            : Lvalue;
        }

        internal sealed record UnaryOperation(Range Location,
            UnaryOperator Operator,
            Expr Operand)
        : Expr;

        internal sealed record BinaryOperation(Range Location,
            Expr Left,
            BinaryOperator Operator,
            Expr Right)
        : Expr;

        internal sealed record BuiltinFdf(Range Location,
            Expr ArgumentNomLog)
        : Expr;

        internal sealed record Call(Range Location,
            Ident Callee,
            IReadOnlyList<ParameterActual> Parameters)
        : Expr;

        internal sealed record ParenExprImpl
        : ParenExpr
        {
            public ParenExprImpl(Range location,
            Expr expr) => (Location, InnerExpr) = (location,
                expr is ParenExpr b ? b.InnerExpr : expr);
            public Range Location { get; }
            public Expr InnerExpr { get; }
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
            Ident Name)
        : Type;

        internal sealed record String(Range Location)
        : Type;

        internal sealed record Array(Range Location,
            Type Type,
            IReadOnlyList<Expr> Dimensions)
        : Type;

        internal sealed record File(Range Location)
        : Type;

        internal sealed record Character(Range Location)
        : Type;

        internal sealed record Boolean(Range Location)
        : Type;

        internal sealed record Integer(Range Location)
        : Type;

        internal sealed record Real(Range Location)
        : Type;

        internal sealed record LengthedString(Range Location,
            Expr Length)
        : Type;

        internal sealed record Structure(Range Location,
            IReadOnlyList<Component> Components)
        : Type;
    }

    internal interface Designator : Node
    {
        internal sealed record Array(Range Location, Expr Index) : Designator;
        internal sealed record Structure(Range Location, Ident Comp) : Designator;
    }
    interface Component : Node;

    internal sealed record Nop(Range Location) : Stmt, Declaration;

    internal sealed record ParameterActual(Range Location,
        ParameterMode Mode,
        Expr Value)
    : Node;

    internal sealed record ParameterFormal(Range Location,
        ParameterMode Mode,
        Ident Name,
        Type Type)
    : Node;

    internal sealed record VariableDeclaration(Range Location,
        IReadOnlyList<Ident> Names,
        Type Type)
    : Component;

    internal sealed record ProcedureSignature(Range Location,
        Ident Name,
        IReadOnlyList<ParameterFormal> Parameters)
    : Node;

    internal sealed record FunctionSignature(Range Location,
        Ident Name,
        IReadOnlyList<ParameterFormal> Parameters,
        Type ReturnType)
    : Node;

    internal abstract record UnaryOperator(Range Location, string Representation) : Node
    {
        public sealed record Cast(Range Location, Type Target) : UnaryOperator(Location, "(cast)");
        public sealed record Minus(Range Location) : UnaryOperator(Location, "-");
        public sealed record Not(Range Location) : UnaryOperator(Location, "NON");
        public sealed record Plus(Range Location) : UnaryOperator(Location, "+");
    }

    internal abstract record BinaryOperator(Range Location, string Representation) : Node
    {
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
    interface CompilerDirective : Declaration, Stmt, Component, Initializer.Braced.Item
    {
        internal sealed record EvalExpr(Range Location, Expr Expr) : CompilerDirective;
        internal sealed record EvalType(Range Location, Type Type) : CompilerDirective;
        internal sealed record Assert(Range Location, Expr Expr, Option<Expr> Message) : CompilerDirective;
    }
}
