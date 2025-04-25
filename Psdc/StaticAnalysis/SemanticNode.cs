using System.Collections.Immutable;
using System.Diagnostics;
using Scover.Psdc.Parsing;
using Scover.Psdc.Pseudocode;

namespace Scover.Psdc.StaticAnalysis;


public interface SemanticNode
{
    Scope Scope { get; }
    public readonly record struct Algorithm(Scope Scope, ImmutableArray<Decl> Decls) : SemanticNode
    {
    }
    interface Decl : SemanticNode, Locatable
    {
        internal readonly record struct ConstantDef : Decl
        {
            internal ConstantDef(Scope scope, FixedRange extent, EvaluatedType type, ImmutableArray<InitDeclarator> declarators)
            {
                Scope = scope;
                Extent = extent;
                Type = type;
                Debug.Assert(declarators.Length > 0);
                Declarators = declarators;
            }
            public Scope Scope { get; }
            public FixedRange Extent { get; }
            public EvaluatedType Type { get; }
            public ImmutableArray<InitDeclarator> Declarators { get; }
        }
        internal readonly record struct TypeDef : Decl
        {
            internal TypeDef(Scope scope, FixedRange extent, EvaluatedType type, ImmutableArray<Ident> names)
            {
                Scope = scope;
                Extent = extent;
                Type = type;
                Debug.Assert(names.Length > 0);
                Names = names;
            }
            public Scope Scope { get; }
            public FixedRange Extent { get; }
            public EvaluatedType Type { get; }
            public ImmutableArray<Ident> Names { get; }
        }
        internal readonly record struct MainProgram(Scope Scope, FixedRange Extent, Ident Title, Stmt.Block Body) : Decl
        {
        }
        internal readonly record struct FuncDecl(Scope Scope, FixedRange Extent, FuncSig Sig) : Decl
        {
        }
        internal readonly record struct FuncDef(Scope Scope, FixedRange Extent, FuncSig Sig, Stmt.Block Body) : Decl
        {
        }
    }
    internal interface CompilerDirective : SemanticNode, Decl, Stmt
    {
        internal readonly record struct Assert(Scope Scope, FixedRange Extent, Expr Expr, Option<Expr> Message) : CompilerDirective
        {
        }
        internal readonly record struct EvalExpr(Scope Scope, FixedRange Extent, Expr Expr) : CompilerDirective
        {
        }
        internal readonly record struct EvalType(Scope Scope, FixedRange Extent, EvaluatedType Type) : CompilerDirective
        {
        }
    }
    internal readonly record struct Nop(Scope Scope, FixedRange Extent) : Decl, Stmt
    {
    }
    internal readonly record struct InitDeclarator(Scope Scope, Ident Name, Init Init) : SemanticNode
    {
    }
    interface Stmt : SemanticNode, Locatable
    {
        internal readonly record struct Block(Scope Scope, FixedRange Extent, ImmutableArray<Stmt> Stmts) : Stmt
        {
        }
        internal readonly record struct Assignment(Scope Scope, FixedRange Extent, Expr.Lvalue Target, Expr Value) : Stmt
        {
        }
        internal readonly record struct WhileLoop(Scope Scope, FixedRange Extent, Expr Condition, ImmutableArray<Stmt> Stmts) : Stmt
        {
        }
        internal readonly record struct DoWhileLoop(Scope Scope, FixedRange Extent, Expr Condition, ImmutableArray<Stmt> Stmts) : Stmt
        {
        }
        internal readonly record struct ForLoop(Scope Scope, FixedRange Extent, Option<Stmt> Initialization, Option<Expr> Condition, Option<Stmt.Assignment> Increment, ImmutableArray<Stmt> Stmts) : Stmt
        {
        }
        internal readonly record struct Return(Scope Scope, FixedRange Extent, Expr Value) : Stmt
        {
        }
        internal readonly record struct Write(Scope Scope, FixedRange Extent, ImmutableArray<Expr> Args) : Stmt
        {
        }
        internal readonly record struct Read(Scope Scope, FixedRange Extent, Expr.Lvalue Target) : Stmt
        {
        }
        internal readonly record struct Trunc(Scope Scope, FixedRange Extent, Expr Arg) : Stmt
        {
        }
        internal readonly record struct LocalVarDecl(Scope Scope, FixedRange Extent, EvaluatedType Type, InitDeclarator Declarators) : Stmt
        {
        }
        internal readonly record struct Alternative(Scope Scope, FixedRange Extent, Clause If, ImmutableArray<Clause> ElseIfs, Option<ImmutableArray<Stmt>> Else) : Stmt
        {
        }
        internal readonly record struct Switch(Scope Scope, FixedRange Extent, Expr Condition, ImmutableArray<Clause> Cases, Option<ImmutableArray<Stmt>> Default) : Stmt
        {
        }
    }
    internal readonly record struct FuncSig(Scope Scope, Ident Name, ImmutableArray<FormalParam> Parameters, Option<EvaluatedType> ReturnType) : SemanticNode
    {
    }
    internal interface Expr : SemanticNode, Stmt, Init
    {
        internal readonly record struct Unary(Value Value, FixedRange Extent, Scope Scope, UnaryOperator Operator, Expr Operand) : Expr
        {
        }
        internal readonly record struct Binary(Value Value, FixedRange Extent, Scope Scope, Expr Left, BinaryOperator Operator, Expr Right) : Expr
        {
        }
        internal readonly record struct Call(Value Value, FixedRange Extent, Scope Scope, Ident Callee, ImmutableArray<ActualParam> Args) : Expr
        {
        }
        internal interface Lvalue : Expr
        {
            internal readonly record struct ComponentAccess(Value Value, FixedRange Extent, Scope Scope, Expr Structure, Ident Name) : Lvalue
            {
            }
            internal readonly record struct ArraySub(Value Value, FixedRange Extent, Scope Scope, Expr Array, Expr Index) : Lvalue
            {
            }
            internal readonly record struct VarRef(Value Value, FixedRange Extent, Scope Scope, bool IsOut, Ident Name) : Lvalue
            {
            }
        }
        internal interface Literal : Expr
        {
            internal readonly record struct True(Scope Scope, FixedRange Extent) : Literal
            {
                static readonly Value value = new BooleanValue(BooleanType.Instance, ValueStatus.Comptime.Of(true));
                public Value Value => value;
            }
            internal readonly record struct False(Scope Scope, FixedRange Extent) : Literal
            {
                static readonly Value value = new BooleanValue(BooleanType.Instance, ValueStatus.Comptime.Of(false));
                public Value Value => value;
            }
            internal readonly record struct String(Value Value, FixedRange Extent, Scope Scope) : Literal
            {
            }
            internal readonly record struct Real(Value Value, FixedRange Extent, Scope Scope) : Literal
            {
            }
            internal readonly record struct Character(Value Value, FixedRange Extent, Scope Scope) : Literal
            {
            }
            internal readonly record struct Integer(Value Value, FixedRange Extent, Scope Scope) : Literal
            {
            }
        }
    }
    internal interface Init : SemanticNode
    {
        Value Value { get; }
        readonly record struct Braced(Value Value, Scope Scope, ImmutableArray<Braced.Item> Items) : Init
        {
            internal interface Item
            {
                internal readonly record struct Value(Scope Scope, ImmutableArray<Designator> Designator, Init Init) : Item
                {
                }
            }
        }
    }
    internal readonly record struct eclarator(Scope Scope, Ident Name, Init Init) : SemanticNode
    {
    }
    internal readonly record struct Clause(Scope Scope, Expr Condition, ImmutableArray<Stmt> Body) : SemanticNode
    {
    }
    internal readonly record struct FormalParam(Scope Scope, Ident Name, bool IsOut, EvaluatedType Type) : SemanticNode
    {
    }
    interface UnaryOperator : SemanticNode
    {
        internal readonly record struct Cast(Scope Scope) : UnaryOperator
        {
        }
        internal readonly record struct Minus(Scope Scope) : UnaryOperator
        {
        }
        internal readonly record struct Not(Scope Scope) : UnaryOperator
        {
        }
        internal readonly record struct Plus(Scope Scope) : UnaryOperator
        {
        }
    }
    interface BinaryOperator : SemanticNode
    {
        internal readonly record struct Add(Scope Scope) : BinaryOperator
        {
        }
        internal readonly record struct And(Scope Scope) : BinaryOperator
        {
        }
        internal readonly record struct Div(Scope Scope) : BinaryOperator
        {
        }
        internal readonly record struct Eq(Scope Scope) : BinaryOperator
        {
        }
        internal readonly record struct Gt(Scope Scope) : BinaryOperator
        {
        }
        internal readonly record struct Ge(Scope Scope) : BinaryOperator
        {
        }
        internal readonly record struct Lt(Scope Scope) : BinaryOperator
        {
        }
        internal readonly record struct Le(Scope Scope) : BinaryOperator
        {
        }
        internal readonly record struct Mod(Scope Scope) : BinaryOperator
        {
        }
        internal readonly record struct Mult(Scope Scope) : BinaryOperator
        {
        }
        internal readonly record struct Ne(Scope Scope) : BinaryOperator
        {
        }
        internal readonly record struct Or(Scope Scope) : BinaryOperator
        {
        }
        internal readonly record struct Sub(Scope Scope) : BinaryOperator
        {
        }
        internal readonly record struct Xor(Scope Scope) : BinaryOperator
        {
        }
    }
    internal readonly record struct ActualParam(Scope Scope, bool IsOut, Expr Value) : SemanticNode
    {
    }
    internal interface Designator : SemanticNode
    {
        internal readonly record struct Component(Scope Scope, Ident Name) : Designator
        {
        }
        internal readonly record struct Indice(Scope Scope, Expr At) : Designator
        {
        }
    }
}
