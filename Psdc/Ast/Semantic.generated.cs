using Scover.Psdc.Parsing;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Scover.Psdc.Ast;

public interface SemanticNode
{
    SemanticMetadata Meta { get; }
    public readonly record struct Algorithm(SemanticMetadata Meta, ImmutableArray<Decl> Decls) : SemanticNode
    {
    }
    public interface Decl : SemanticNode
    {
        public readonly record struct ConstantDef : Decl
        {
            public ConstantDef(SemanticMetadata meta, Type type, ImmutableArray<InitDeclarator> declarators)
            {
                Meta = meta;
                Type = type;
                Debug.Assert(declarators.Length > 0);
                Declarators = declarators;
            }
            public SemanticMetadata Meta { get; }
            public Type Type { get; }
            public ImmutableArray<InitDeclarator> Declarators { get; }
        }
        public readonly record struct TypeDef : Decl
        {
            public TypeDef(SemanticMetadata meta, Type type, ImmutableArray<Ident> names)
            {
                Meta = meta;
                Type = type;
                Debug.Assert(names.Length > 0);
                Names = names;
            }
            public SemanticMetadata Meta { get; }
            public Type Type { get; }
            public ImmutableArray<Ident> Names { get; }
        }
        public readonly record struct MainProgram(SemanticMetadata Meta, Ident Title, Stmt.Block Body) : Decl
        {
        }
        public readonly record struct FuncDecl(SemanticMetadata Meta, FuncSig Sig) : Decl
        {
        }
        public readonly record struct FuncDef(SemanticMetadata Meta, FuncSig Sig, Stmt.Block Body) : Decl
        {
        }
    }
    public interface CompilerDirective : SemanticNode, Decl, Stmt
    {
        public readonly record struct Assert(SemanticMetadata Meta, Expr Expr, Option<Expr> Message) : CompilerDirective
        {
        }
        public readonly record struct EvalExpr(SemanticMetadata Meta, Expr Expr) : CompilerDirective
        {
        }
        public readonly record struct EvalType(SemanticMetadata Meta, Type Type) : CompilerDirective
        {
        }
    }
    public readonly record struct Nop(SemanticMetadata Meta) : SemanticNode, Decl, Stmt
    {
    }
    public interface Type : SemanticNode
    {
        public readonly record struct Aliased(SemanticMetadata Meta, Ident Name) : Type
        {
        }
        public readonly record struct Boolean(SemanticMetadata Meta) : Type
        {
        }
        public readonly record struct Character(SemanticMetadata Meta) : Type
        {
        }
        public readonly record struct Integer(SemanticMetadata Meta) : Type
        {
        }
        public readonly record struct Real(SemanticMetadata Meta) : Type
        {
        }
        public readonly record struct String(SemanticMetadata Meta, Option<Expr> Length) : Type
        {
        }
        public readonly record struct Array : Type
        {
            public Array(SemanticMetadata meta, Type itemType, ImmutableArray<Expr> dimensions)
            {
                Meta = meta;
                ItemType = itemType;
                Debug.Assert(dimensions.Length > 0);
                Dimensions = dimensions;
            }
            public SemanticMetadata Meta { get; }
            public Type ItemType { get; }
            public ImmutableArray<Expr> Dimensions { get; }
        }
        public readonly record struct Structure(SemanticMetadata Meta, ImmutableArray<Structure.Member> Members) : Type
        {
            public interface Member
            {
                public readonly record struct Component : Member, Designator
                {
                    public Component(SemanticMetadata meta, Type type, ImmutableArray<Ident> names)
                    {
                        Meta = meta;
                        Type = type;
                        Debug.Assert(names.Length > 0);
                        Names = names;
                    }
                    public SemanticMetadata Meta { get; }
                    public Type Type { get; }
                    public ImmutableArray<Ident> Names { get; }
                }
            }
        }
    }
    public readonly record struct InitDeclarator(SemanticMetadata Meta, Ident Name, Init Init) : SemanticNode
    {
    }
    public interface Stmt : SemanticNode
    {
        public readonly record struct Block(SemanticMetadata Meta, ImmutableArray<Stmt> Stmts) : Stmt
        {
        }
        public readonly record struct Assignment(SemanticMetadata Meta, Expr.Lvalue Target, Expr Value) : Stmt
        {
        }
        public readonly record struct WhileLoop(SemanticMetadata Meta, Expr Condition, ImmutableArray<Stmt> Stmts) : Stmt
        {
        }
        public readonly record struct DoWhileLoop(SemanticMetadata Meta, Expr Condition, ImmutableArray<Stmt> Stmts) : Stmt
        {
        }
        public readonly record struct ForLoop(SemanticMetadata Meta, Option<Stmt> Initialization, Option<Expr> Condition, Option<Stmt.Assignment> Increment, ImmutableArray<Stmt> Stmts) : Stmt
        {
        }
        public readonly record struct Return(SemanticMetadata Meta, Expr Value) : Stmt
        {
        }
        public readonly record struct Write(SemanticMetadata Meta, ImmutableArray<Expr> Args) : Stmt
        {
        }
        public readonly record struct Read(SemanticMetadata Meta, Expr.Lvalue Target) : Stmt
        {
        }
        public readonly record struct Trunc(SemanticMetadata Meta, Expr Arg) : Stmt
        {
        }
        public readonly record struct LocalVarDecl(SemanticMetadata Meta, Type Type, InitDeclarator Declarators) : Stmt
        {
        }
        public readonly record struct Alternative(SemanticMetadata Meta, Clause If, ImmutableArray<Clause> ElseIfs, Option<ImmutableArray<Stmt>> Else) : Stmt
        {
        }
        public readonly record struct Switch(SemanticMetadata Meta, Expr Condition, ImmutableArray<Clause> Cases, Option<ImmutableArray<Stmt>> Default) : Stmt
        {
        }
    }
    public readonly record struct FuncSig(SemanticMetadata Meta, Ident Name, ImmutableArray<FormalParam> Parameters, Option<Type> ReturnType) : SemanticNode
    {
    }
    public interface Expr : SemanticNode, Stmt, Init
    {
        public readonly record struct Unary(SemanticMetadata Meta, UnaryOperator Operator, Expr Operand) : Expr
        {
        }
        public readonly record struct Binary(SemanticMetadata Meta, Expr Left, BinaryOperator Operator, Expr Right) : Expr
        {
        }
        public readonly record struct Call(SemanticMetadata Meta, Ident Callee, ImmutableArray<ActualParam> Args) : Expr
        {
        }
        public interface Lvalue : Expr
        {
            public readonly record struct ComponentAccess(SemanticMetadata Meta, Expr Structure, Ident Name) : Lvalue
            {
            }
            public readonly record struct ArraySub(SemanticMetadata Meta, Expr Array, Expr Index) : Lvalue
            {
            }
            public readonly record struct VarRef(SemanticMetadata Meta, bool IsOut, Ident Name) : Lvalue
            {
            }
        }
        public interface Literal : Expr
        {
            public readonly record struct True(SemanticMetadata Meta) : Literal
            {
            }
            public readonly record struct False(SemanticMetadata Meta) : Literal
            {
            }
            public readonly record struct String(SemanticMetadata Meta) : Literal, Type
            {
            }
            public readonly record struct Real(SemanticMetadata Meta) : Literal, Type
            {
            }
            public readonly record struct Character(SemanticMetadata Meta) : Literal, Type
            {
            }
            public readonly record struct Integer(SemanticMetadata Meta) : Literal, Type
            {
            }
        }
    }
    public interface Init : SemanticNode
    {
        public readonly record struct Braced(SemanticMetadata Meta, ImmutableArray<Braced.Item> Items) : Init
        {
            public interface Item
            {
                public readonly record struct Value(SemanticMetadata Meta, ImmutableArray<Designator> Designator, Init Init) : Item
                {
                }
            }
        }
    }
    public readonly record struct Declarator(SemanticMetadata Meta, Ident Name, Init Init) : SemanticNode
    {
    }
    public readonly record struct Clause(SemanticMetadata Meta, Expr Condition, ImmutableArray<Stmt> Body) : SemanticNode
    {
    }
    public readonly record struct FormalParam(SemanticMetadata Meta) : SemanticNode
    {
    }
    public interface UnaryOperator : SemanticNode
    {
        public readonly record struct Cast(SemanticMetadata Meta) : UnaryOperator
        {
        }
        public readonly record struct Minus(SemanticMetadata Meta) : UnaryOperator
        {
        }
        public readonly record struct Not(SemanticMetadata Meta) : UnaryOperator
        {
        }
        public readonly record struct Plus(SemanticMetadata Meta) : UnaryOperator
        {
        }
    }
    public interface BinaryOperator : SemanticNode
    {
        public readonly record struct Add(SemanticMetadata Meta) : BinaryOperator
        {
        }
        public readonly record struct And(SemanticMetadata Meta) : BinaryOperator
        {
        }
        public readonly record struct Div(SemanticMetadata Meta) : BinaryOperator
        {
        }
        public readonly record struct Eq(SemanticMetadata Meta) : BinaryOperator
        {
        }
        public readonly record struct Gt(SemanticMetadata Meta) : BinaryOperator
        {
        }
        public readonly record struct Ge(SemanticMetadata Meta) : BinaryOperator
        {
        }
        public readonly record struct Lt(SemanticMetadata Meta) : BinaryOperator
        {
        }
        public readonly record struct Le(SemanticMetadata Meta) : BinaryOperator
        {
        }
        public readonly record struct Mod(SemanticMetadata Meta) : BinaryOperator
        {
        }
        public readonly record struct Mult(SemanticMetadata Meta) : BinaryOperator
        {
        }
        public readonly record struct Ne(SemanticMetadata Meta) : BinaryOperator
        {
        }
        public readonly record struct Or(SemanticMetadata Meta) : BinaryOperator
        {
        }
        public readonly record struct Sub(SemanticMetadata Meta) : BinaryOperator
        {
        }
        public readonly record struct Xor(SemanticMetadata Meta) : BinaryOperator
        {
        }
    }
    public readonly record struct ActualParam(SemanticMetadata Meta, bool IsOut, Expr Value) : SemanticNode
    {
    }
    public interface Designator : SemanticNode
    {
        public readonly record struct Component(SemanticMetadata Meta, Ident Name) : Designator
        {
        }
        public readonly record struct Indice(SemanticMetadata Meta, Expr At) : Designator
        {
        }
    }
}
