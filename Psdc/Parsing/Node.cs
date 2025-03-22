using Scover.Psdc.Pseudocode;

namespace Scover.Psdc.Parsing;

public interface Node
{
    FixedRange Location { get; }

    internal interface ParenExpr : Expr
    {
        Expr InnerExpr { get; }
    }
    sealed record Algorithm(FixedRange Location, IReadOnlyList<CompilerDirective> LeadingDirectives, Ident Title, IReadOnlyList<Declaration> Declarations) : Node;
    interface Declaration : Node
    {
        internal sealed record MainProgram(FixedRange Location, IReadOnlyList<Stmt> Body) : Declaration;
        internal sealed record TypeAlias(FixedRange Location, Ident Name, Type Type) : Declaration;
        internal sealed record Constant(FixedRange Location, Type Type, Ident Name, Initializer Value) : Declaration;
        internal sealed record Procedure(FixedRange Location, ProcedureSignature Signature) : Declaration;
        internal sealed record ProcedureDefinition(FixedRange Location, ProcedureSignature Signature, IReadOnlyList<Stmt> Body) : Declaration;
        internal sealed record Function(FixedRange Location, FunctionSignature Signature) : Declaration;
        internal sealed record FunctionDefinition(FixedRange Location, FunctionSignature Signature, IReadOnlyList<Stmt> Body) : Declaration;
    }
    interface Stmt : Node
    {
        internal sealed record ExprStmt(FixedRange Location, Expr Expr) : Stmt;
        internal sealed record Assignment(FixedRange Location, Expr.Lvalue Target, Expr Value) : Stmt;
        internal sealed record DoWhileLoop(FixedRange Location, Expr Condition, IReadOnlyList<Stmt> Body) : Stmt;

        internal sealed record Alternative(
            FixedRange Location,
            Alternative.IfClause If,
            IReadOnlyList<Alternative.ElseIfClause> ElseIfs,
            Option<Alternative.ElseClause> Else
        ) : Stmt
        {
            internal sealed record IfClause(FixedRange Location, Expr Condition, IReadOnlyList<Stmt> Body) : Node;
            internal sealed record ElseIfClause(FixedRange Location, Expr Condition, IReadOnlyList<Stmt> Body) : Node;
            internal sealed record ElseClause(FixedRange Location, IReadOnlyList<Stmt> Body) : Node;
        }

        internal sealed record Switch(FixedRange Location, Expr Expr, IReadOnlyList<Switch.Case> Cases) : Stmt
        {
            internal interface Case : Node
            {
                IReadOnlyList<Stmt> Body { get; }
                internal sealed record OfValue(FixedRange Location, Expr Value, IReadOnlyList<Stmt> Body) : Case;
                internal sealed record Default(FixedRange Location, IReadOnlyList<Stmt> Body) : Case;
            }
        }

        internal interface Builtin : Stmt
        {
            internal sealed record Ecrire(FixedRange Location, Expr ArgumentNomLog, Expr ArgumentExpression) : Builtin;
            internal sealed record Fermer(FixedRange Location, Expr ArgumentNomLog) : Builtin;
            internal sealed record Lire(FixedRange Location, Expr ArgumentNomLog, Expr.Lvalue ArgumentVariable) : Builtin;
            internal sealed record OuvrirAjout(FixedRange Location, Expr ArgumentNomLog) : Builtin;
            internal sealed record OuvrirEcriture(FixedRange Location, Expr ArgumentNomLog) : Builtin;
            internal sealed record OuvrirLecture(FixedRange Location, Expr ArgumentNomLog) : Builtin;
            internal sealed record Assigner(FixedRange Location, Expr.Lvalue ArgumentNomLog, Expr ArgumentNomExt) : Builtin;
            internal sealed record EcrireEcran(FixedRange Location, IReadOnlyList<Expr> Arguments) : Builtin;
            internal sealed record LireClavier(FixedRange Location, Expr.Lvalue ArgumentVariable) : Builtin;
        }

        internal sealed record ForLoop(FixedRange Location, Expr.Lvalue Variant, Expr Start, Expr End, Option<Expr> Step, IReadOnlyList<Stmt> Body) : Stmt;

        internal sealed record RepeatLoop(FixedRange Location, Expr Condition, IReadOnlyList<Stmt> Body) : Stmt;

        internal sealed record Return(FixedRange Location, Option<Expr> Value) : Stmt;

        internal sealed record LocalVariable(FixedRange Location, VariableDeclaration Decl, Option<Initializer> Value) : Stmt;

        internal sealed record WhileLoop(FixedRange Location, Expr Condition, IReadOnlyList<Stmt> Body) : Stmt;
    }
    interface Initializer : Node
    {
        sealed record Braced(FixedRange Location, IReadOnlyList<Braced.Item> Items) : Initializer
        {
            public interface Item : Node;
            internal sealed record ValuedItem(FixedRange Location, IReadOnlyList<Designator> Designators, Initializer Value) : Item;
        }
    }

    internal interface Expr : Initializer
    {
        internal interface Lvalue : Expr
        {
            internal sealed record ComponentAccess(FixedRange Location, Expr Structure, Ident ComponentName) : Lvalue;

            internal sealed record ParenLValue : Lvalue, ParenExpr
            {
                public ParenLValue(FixedRange location, Lvalue lvalue) =>
                    (Location, ContainedLvalue) = (location, lvalue is ParenExpr { InnerExpr: Lvalue l } ? l : lvalue);
                public FixedRange Location { get; }
                public Lvalue ContainedLvalue { get; }
                Expr ParenExpr.InnerExpr => ContainedLvalue;
            }

            internal sealed record ArraySubscript(FixedRange Location, Expr Array, Expr Index) : Lvalue;

            internal sealed record VariableReference(FixedRange Location, Ident Name) : Lvalue;
        }

        internal sealed record UnaryOperation(FixedRange Location, UnaryOperator Operator, Expr Operand) : Expr;

        internal sealed record BinaryOperation(FixedRange Location, Expr Left, BinaryOperator Operator, Expr Right) : Expr;

        internal sealed record BuiltinFdf(FixedRange Location, Expr ArgumentNomLog) : Expr;

        internal sealed record Call(FixedRange Location, Ident Callee, IReadOnlyList<ParameterActual> Parameters) : Expr;

        internal sealed record ParenExprImpl : ParenExpr
        {
            public ParenExprImpl(FixedRange location, Expr expr) => (Location, InnerExpr) = (location, expr is ParenExpr b ? b.InnerExpr : expr);
            public FixedRange Location { get; }
            public Expr InnerExpr { get; }
        }

        internal abstract record Literal<TType, TValue, TUnderlying>(FixedRange Location, TType ValueType, TUnderlying Value) : Literal
        where TValue : Value where TType : EvaluatedType, InstantiableType<TValue, TUnderlying> where TUnderlying : notnull
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

            internal sealed record True(FixedRange Location) : Literal<BooleanType, BooleanValue, bool>(Location, BooleanType.Instance, true);

            internal sealed record False(FixedRange Location) : Literal<BooleanType, BooleanValue, bool>(Location, BooleanType.Instance, false);

            internal sealed record Character(FixedRange Location, char Value)
                : Literal<CharacterType, CharacterValue, char>(Location, CharacterType.Instance, Value);

            internal sealed record Integer(FixedRange Location, int Value) : Literal<IntegerType, IntegerValue, int>(Location, IntegerType.Instance, Value)
            {
                public Integer(FixedRange location, string valueStr) : this(location, int.Parse(valueStr, Format.Code)) { }
            }

            internal sealed record Real(FixedRange Location, decimal Value) : Literal<RealType, RealValue, decimal>(Location, RealType.Instance, Value)
            {
                public Real(FixedRange location, string valueStr) : this(location, decimal.Parse(valueStr, Format.Code)) { }
            }

            internal sealed record String(FixedRange Location, string Value)
                : Literal<LengthedStringType, LengthedStringValue, string>(Location, LengthedStringType.Create(Value.Length), Value);
        }
    }

    internal interface Type : Node
    {
        internal sealed record AliasReference(FixedRange Location, Ident Name) : Type;

        internal sealed record String(FixedRange Location) : Type;

        internal sealed record Array(FixedRange Location, Type Type, IReadOnlyList<Expr> Dimensions) : Type;

        internal sealed record Character(FixedRange Location) : Type;

        internal sealed record Boolean(FixedRange Location) : Type;

        internal sealed record Integer(FixedRange Location) : Type;

        internal sealed record Real(FixedRange Location) : Type;

        internal sealed record LengthedString(FixedRange Location, Expr Length) : Type;

        internal sealed record Structure(FixedRange Location, IReadOnlyList<Component> Components) : Type;
    }

    internal interface Designator : Node
    {
        internal sealed record Array(FixedRange Location, Expr Index) : Designator;
        internal sealed record Structure(FixedRange Location, Ident Comp) : Designator;
    }
    interface Component : Node;

    internal sealed record Nop(FixedRange Location) : Stmt, Declaration;

    internal sealed record ParameterActual(FixedRange Location, ParameterMode Mode, Expr Value) : Node;

    internal sealed record ParameterFormal(FixedRange Location, ParameterMode Mode, Ident Name, Type Type) : Node;

    internal sealed record VariableDeclaration(FixedRange Location, IReadOnlyList<Ident> Names, Type Type) : Component;

    internal sealed record ProcedureSignature(FixedRange Location, Ident Name, IReadOnlyList<ParameterFormal> Parameters) : Node;

    internal sealed record FunctionSignature(FixedRange Location, Ident Name, IReadOnlyList<ParameterFormal> Parameters, Type ReturnType) : Node;

    internal abstract record UnaryOperator(FixedRange Location, string Representation) : Node
    {
        public sealed record Cast(FixedRange Location, Type Target) : UnaryOperator(Location, "(cast)");
        public sealed record Minus(FixedRange Location) : UnaryOperator(Location, "-");
        public sealed record Not(FixedRange Location) : UnaryOperator(Location, "NON");
        public sealed record Plus(FixedRange Location) : UnaryOperator(Location, "+");
    }

    internal abstract record BinaryOperator(FixedRange Location, string Representation) : Node
    {
        public sealed record Add(FixedRange Location) : BinaryOperator(Location, "ET");
        public sealed record And(FixedRange Location) : BinaryOperator(Location, "/");
        public sealed record Divide(FixedRange Location) : BinaryOperator(Location, "==");
        public sealed record Equal(FixedRange Location) : BinaryOperator(Location, ">");
        public sealed record GreaterThan(FixedRange Location) : BinaryOperator(Location, ">=");
        public sealed record GreaterThanOrEqual(FixedRange Location) : BinaryOperator(Location, "<");
        public sealed record LessThan(FixedRange Location) : BinaryOperator(Location, "<=");
        public sealed record LessThanOrEqual(FixedRange Location) : BinaryOperator(Location, "-");
        public sealed record Mod(FixedRange Location) : BinaryOperator(Location, "%");
        public sealed record Multiply(FixedRange Location) : BinaryOperator(Location, "*");
        public sealed record NotEqual(FixedRange Location) : BinaryOperator(Location, "!=");
        public sealed record Or(FixedRange Location) : BinaryOperator(Location, "OU");
        public sealed record Subtract(FixedRange Location) : BinaryOperator(Location, "+");
        public sealed record Xor(FixedRange Location) : BinaryOperator(Location, "XOR");
    }
    interface CompilerDirective : Declaration, Stmt, Component, Initializer.Braced.Item
    {
        internal sealed record EvalExpr(FixedRange Location, Expr Expr) : CompilerDirective;
        internal sealed record EvalType(FixedRange Location, Type Type) : CompilerDirective;
        internal sealed record Assert(FixedRange Location, Expr Expr, Option<Expr> Message) : CompilerDirective;
    }
}
