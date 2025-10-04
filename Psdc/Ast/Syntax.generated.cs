using System.Collections.Immutable;
using System.Diagnostics;

namespace Scover.Psdc.Ast;

public interface Node
{
    public readonly record struct Algorithm(ImmutableArray<Decl> Decls) : Node
    {
    }
    public interface Decl : Node
    {
        public readonly record struct ConstantDef : Decl
        {
            public ConstantDef(Type type, ImmutableArray<InitDeclarator> declarators)
            {
                Type = type;
                Debug.Assert(declarators.Length > 0);
                Declarators = declarators;
            }
            public Type Type { get; }
            public ImmutableArray<InitDeclarator> Declarators { get; }
        }
        public readonly record struct TypeDef : Decl
        {
            public TypeDef(Type type, ImmutableArray<Ident> names)
            {
                Type = type;
                Debug.Assert(names.Length > 0);
                Names = names;
            }
            public Type Type { get; }
            public ImmutableArray<Ident> Names { get; }
        }
        public readonly record struct MainProgram(Ident Title, Stmt.Block Body) : Decl
        {
        }
        public readonly record struct FuncDecl(FuncSig Sig) : Decl
        {
        }
        public readonly record struct FuncDef(FuncSig Sig, Stmt.Block Body) : Decl
        {
        }
    }
    public interface CompilerDirective : Node, Decl, Stmt
    {
        public readonly record struct Assert(Expr Expr, Option<Expr> Message) : CompilerDirective
        {
        }
        public readonly record struct EvalExpr(Expr Expr) : CompilerDirective
        {
        }
        public readonly record struct EvalType(Type Type) : CompilerDirective
        {
        }
    }
    public readonly record struct Nop : Node, Decl, Stmt
    {
    }
    public interface Type : Node
    {
        public readonly record struct Aliased(Ident Name) : Type
        {
        }
        public readonly record struct Boolean : Type
        {
        }
        public readonly record struct Character : Type
        {
        }
        public readonly record struct Integer : Type
        {
        }
        public readonly record struct Real : Type
        {
        }
        public readonly record struct String(Option<Expr> Length) : Type
        {
        }
        public readonly record struct Array : Type
        {
            public Array(Type itemType, ImmutableArray<Expr> dimensions)
            {
                ItemType = itemType;
                Debug.Assert(dimensions.Length > 0);
                Dimensions = dimensions;
            }
            public Type ItemType { get; }
            public ImmutableArray<Expr> Dimensions { get; }
        }
        public readonly record struct Structure(ImmutableArray<Structure.Member> Members) : Type
        {
            public interface Member
            {
                public readonly record struct Component : Member, Designator
                {
                    public Component(Type type, ImmutableArray<Ident> names)
                    {
                        Type = type;
                        Debug.Assert(names.Length > 0);
                        Names = names;
                    }
                    public Type Type { get; }
                    public ImmutableArray<Ident> Names { get; }
                }
            }
        }
    }
    public readonly record struct InitDeclarator(Ident Name, Init Init) : Node
    {
    }
    public interface Stmt : Node
    {
        public readonly record struct Block(ImmutableArray<Stmt> Stmts) : Stmt
        {
        }
        public readonly record struct Assignment(Expr.Lvalue Target, Expr Value) : Stmt
        {
        }
        public readonly record struct WhileLoop(Expr Condition, ImmutableArray<Stmt> Stmts) : Stmt
        {
        }
        public readonly record struct DoWhileLoop(Expr Condition, ImmutableArray<Stmt> Stmts) : Stmt
        {
        }
        public readonly record struct ForLoop(Option<Stmt> Initialization, Option<Expr> Condition, Option<Stmt.Assignment> Increment, ImmutableArray<Stmt> Stmts) : Stmt
        {
        }
        public readonly record struct Return(Expr Value) : Stmt
        {
        }
        public readonly record struct Write(ImmutableArray<Expr> Args) : Stmt
        {
        }
        public readonly record struct Read(Expr.Lvalue Target) : Stmt
        {
        }
        public readonly record struct Trunc(Expr Arg) : Stmt
        {
        }
        public readonly record struct LocalVarDecl(Type Type, InitDeclarator Declarators) : Stmt
        {
        }
        public readonly record struct Alternative(Clause If, ImmutableArray<Clause> ElseIfs, Option<ImmutableArray<Stmt>> Else) : Stmt
        {
        }
        public readonly record struct Switch(Expr Condition, ImmutableArray<Clause> Cases, Option<ImmutableArray<Stmt>> Default) : Stmt
        {
        }
    }
    public readonly record struct FuncSig(Ident Name, ImmutableArray<FormalParam> Parameters, Option<Type> ReturnType) : Node
    {
    }
    public interface Expr : Node, Stmt, Init
    {
        public readonly record struct Unary(UnaryOperator Operator, Expr Operand) : Expr
        {
        }
        public readonly record struct Binary(Expr Left, BinaryOperator Operator, Expr Right) : Expr
        {
        }
        public readonly record struct Call(Ident Callee, ImmutableArray<ActualParam> Args) : Expr
        {
        }
        public interface Lvalue : Expr
        {
            public readonly record struct ComponentAccess(Expr Structure, Ident Name) : Lvalue
            {
            }
            public readonly record struct ArraySub(Expr Array, Expr Index) : Lvalue
            {
            }
            public readonly record struct VarRef(bool IsOut, Ident Name) : Lvalue
            {
            }
        }
        public interface Literal : Expr
        {
            public readonly record struct True : Literal
            {
            }
            public readonly record struct False : Literal
            {
            }
            public readonly record struct String : Literal, Type
            {
            }
            public readonly record struct Real : Literal, Type
            {
            }
            public readonly record struct Character : Literal, Type
            {
            }
            public readonly record struct Integer : Literal, Type
            {
            }
        }
    }
    public interface Init : Node
    {
        public readonly record struct Braced(ImmutableArray<Braced.Item> Items) : Init
        {
            public interface Item
            {
                public readonly record struct Value(ImmutableArray<Designator> Designator, Init Init) : Item
                {
                }
            }
        }
    }
    public readonly record struct Declarator(Ident Name, Init Init) : Node
    {
    }
    public readonly record struct Clause(Expr Condition, ImmutableArray<Stmt> Body) : Node
    {
    }
    public readonly record struct FormalParam : Node
    {
    }
    public interface UnaryOperator : Node
    {
        public readonly record struct Cast : UnaryOperator
        {
        }
        public readonly record struct Minus : UnaryOperator
        {
        }
        public readonly record struct Not : UnaryOperator
        {
        }
        public readonly record struct Plus : UnaryOperator
        {
        }
    }
    public interface BinaryOperator : Node
    {
        public readonly record struct Add : BinaryOperator
        {
        }
        public readonly record struct And : BinaryOperator
        {
        }
        public readonly record struct Div : BinaryOperator
        {
        }
        public readonly record struct Eq : BinaryOperator
        {
        }
        public readonly record struct Gt : BinaryOperator
        {
        }
        public readonly record struct Ge : BinaryOperator
        {
        }
        public readonly record struct Lt : BinaryOperator
        {
        }
        public readonly record struct Le : BinaryOperator
        {
        }
        public readonly record struct Mod : BinaryOperator
        {
        }
        public readonly record struct Mult : BinaryOperator
        {
        }
        public readonly record struct Ne : BinaryOperator
        {
        }
        public readonly record struct Or : BinaryOperator
        {
        }
        public readonly record struct Sub : BinaryOperator
        {
        }
        public readonly record struct Xor : BinaryOperator
        {
        }
    }
    public readonly record struct ActualParam(bool IsOut, Expr Value) : Node
    {
    }
    public interface Designator : Node
    {
        public readonly record struct Component(Ident Name) : Designator
        {
        }
        public readonly record struct Indice(Expr At) : Designator
        {
        }
    }
}
