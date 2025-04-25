using System.Collections.Immutable;
using System.Diagnostics;

namespace Scover.Psdc.Parsing;

public interface Node
{
    readonly record struct Algorithm(ImmutableArray<Decl> Decls) : Node { }
    interface Decl : Node, Locatable
    {
        public readonly record struct ConstantDef : Decl
        {
            public ConstantDef(FixedRange extent, Type type, ImmutableArray<InitDeclarator> declarators)
            {
                Extent = extent;
                Type = type;
                Debug.Assert(declarators.Length > 0);
                Declarators = declarators;
            }
            public FixedRange Extent { get; }
            public Type Type { get; }
            public ImmutableArray<InitDeclarator> Declarators { get; }
        }
        public readonly record struct TypeDef : Decl
        {
            public TypeDef(FixedRange extent, Type type, ImmutableArray<Ident> names)
            {
                Extent = extent;
                Type = type;
                Debug.Assert(names.Length > 0);
                Names = names;
            }
            public FixedRange Extent { get; }
            public Type Type { get; }
            public ImmutableArray<Ident> Names { get; }
        }
        readonly record struct MainProgram(FixedRange Extent, Ident Title, Stmt.Block Body) : Decl { }
        readonly record struct FuncDecl(FixedRange Extent, FuncSig Sig) : Decl { }
        readonly record struct FuncDef(FixedRange Extent, FuncSig Sig, Stmt.Block Body) : Decl
        { }
    }
    interface CompilerDirective : Decl, Stmt, Type.Structure.Member
    {
        readonly record struct Assert(FixedRange Extent, Expr Expr, Option<Expr> Message) : CompilerDirective { }
        readonly record struct EvalExpr(FixedRange Extent, Expr Expr) : CompilerDirective { }
        readonly record struct EvalType(FixedRange Extent, Type Type) : CompilerDirective { }
    }
    readonly record struct Nop(FixedRange Extent) : Decl, Stmt { }
    interface Type : Node, Locatable
    {
        readonly record struct Aliased(FixedRange Extent, Ident Name) : Type { }
        readonly record struct Boolean(FixedRange Extent) : Type { }
        readonly record struct Character(FixedRange Extent) : Type { }
        readonly record struct Integer(FixedRange Extent) : Type { }
        readonly record struct Real(FixedRange Extent) : Type { }
        readonly record struct String(FixedRange Extent, Option<Expr> Length) : Type { }
        public readonly record struct Array : Type
        {
            public Array(FixedRange extent, Type itemType, ImmutableArray<Expr> dimensions)
            {
                Extent = extent;
                ItemType = itemType;
                Debug.Assert(dimensions.Length > 0);
                Dimensions = dimensions;
            }
            public FixedRange Extent { get; }
            public Type ItemType { get; }
            public ImmutableArray<Expr> Dimensions { get; }
        }
        readonly record struct Structure(FixedRange Extent, ImmutableArray<Structure.Member> Members) : Type
        {
            public interface Member : Node, Locatable
            {
                public readonly record struct Component : Member, Designator
                {
                    public Component(FixedRange extent, Type type, ImmutableArray<Ident> names)
                    {
                        Extent = extent;
                        Type = type;
                        Debug.Assert(names.Length > 0);
                        Names = names;
                    }
                    public FixedRange Extent { get; }
                    public Type Type { get; }
                    public ImmutableArray<Ident> Names { get; }
                }
            }
        }
    }
    readonly record struct InitDeclarator(Ident Name, Init Init) : Node { }
    interface Stmt : Node, Locatable
    {
        readonly record struct Block(FixedRange Extent, ImmutableArray<Stmt> Stmts) : Stmt { }
        readonly record struct Assignment(FixedRange Extent, Expr.Lvalue Target, Expr Value) : Stmt { }
        readonly record struct WhileLoop(FixedRange Extent, Expr Condition, ImmutableArray<Stmt> Stmts) : Stmt { }
        readonly record struct DoWhileLoop(FixedRange Extent, Expr Condition, ImmutableArray<Stmt> Stmts) : Stmt { }
        readonly record struct ForLoop(FixedRange Extent, Option<Stmt> Initialization, Option<Expr> Condition, Option<Assignment> Increment, ImmutableArray<Stmt> Body) : Stmt { }
        readonly record struct Return(FixedRange Extent, Expr Value) : Stmt { }
        readonly record struct Write(FixedRange Extent, ImmutableArray<Expr> Args) : Stmt { }
        readonly record struct Read(FixedRange Extent, Expr.Lvalue Target) : Stmt { }
        readonly record struct Trunc(FixedRange Extent, Expr Arg) : Stmt { }
        readonly record struct LocalVarDecl(FixedRange Extent, Type Type, InitDeclarator Declarators) : Stmt { }
        readonly record struct Alternative(FixedRange Extent, Clause If, ImmutableArray<Clause> ElseIfs, Option<ImmutableArray<Stmt>> Else) : Stmt { }
        readonly record struct Switch(FixedRange Extent, Expr Condition, ImmutableArray<Clause> Cases, Option<ImmutableArray<Stmt>> Default) : Stmt { }
    }
    readonly record struct FuncSig(Ident Name, ImmutableArray<FormalParam> Parameters, Option<Type> ReturnType) : Node { }
    interface Expr : Stmt, Init
    {
        readonly record struct Unary(FixedRange Extent, UnaryOperator Operator, Expr Operand) : Expr { }
        readonly record struct Binary(FixedRange Extent, Expr Left, BinaryOperator Operator, Expr Right) : Expr { }
        readonly record struct Call(FixedRange Extent, Ident Callee, ImmutableArray<ActualParam> Args) : Expr { }
        interface Lvalue : Expr
        {
            readonly record struct ComponentAccess(FixedRange Extent, Expr Structure, Ident Name) : Lvalue { }
            readonly record struct ArraySub(FixedRange Extent, Expr Array, Expr Index) : Lvalue { }
            readonly record struct VarRef(FixedRange Extent, bool IsOut, Ident Name) : Lvalue { }
        }
        readonly record struct LitTrue(FixedRange Extent) : Expr { }
        readonly record struct LitFalse(FixedRange Extent) : Expr { }
        readonly record struct LitString(FixedRange Extent) : Expr { }
        readonly record struct LitReal(FixedRange Extent) : Expr { }
        readonly record struct LitCharacter(FixedRange Extent) : Expr { }
        readonly record struct LitInteger(FixedRange Extent) : Expr { }
    }
    interface Init : Node, Locatable
    {
        readonly record struct Braced(FixedRange Extent, ImmutableArray<Braced.Item> Items) : Init
        {
            public interface Item : Node
            {
                readonly record struct Value(ImmutableArray<Designator> Designators, Init Init) : Item { }
            }
        }
    }
    readonly record struct Clause(Expr Condition, ImmutableArray<Stmt> Stmts) : Node { }
    readonly record struct FormalParam : Node { }
    interface UnaryOperator : Node
    {
        readonly record struct Cast : UnaryOperator { }
        readonly record struct Minus : UnaryOperator { }
        readonly record struct Not : UnaryOperator { }
        readonly record struct Plus : UnaryOperator { }
    }

    readonly record struct ActualParam(bool IsOut, Expr Value) : Node { }
    interface Designator : Node
    {
        readonly record struct Component(Ident Name) : Designator { }
        readonly record struct Indice(Expr At) : Designator { }
    }
}
