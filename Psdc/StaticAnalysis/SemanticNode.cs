using System.Collections.Immutable;
using System.Diagnostics;

using Scover.Psdc.Parsing;

namespace Scover.Psdc.StaticAnalysis;

public interface SemanticNode
{
    SemanticMetadata Meta { get; }
    readonly record struct Algorithm : SemanticNode
    {
        public Algorithm(SemanticMetadata meta, ImmutableArray<Decl> decls)
        {
            Meta = meta;
            Debug.Assert(decls.Length > 0);
            Decls = decls;
        }
        public SemanticMetadata Meta { get; }
        public ImmutableArray<Decl> Decls { get; }
    }
    interface Decl : SemanticNode
    {
        readonly record struct ConstantDef(SemanticMetadata Meta, Type Type, ImmutableArray<InitDeclarator> Declarators) : Decl { }
        readonly record struct TypeDef(SemanticMetadata Meta, Type Type, ImmutableArray<Ident> Names) : Decl { }
        readonly record struct MainProgram : Decl
        {
            public MainProgram(SemanticMetadata meta, Ident title, ImmutableArray<Stmt> body)
            {
                Meta = meta;
                Title = title;
                Debug.Assert(body.Length > 0);
                Body = body;
            }
            public SemanticMetadata Meta { get; }
            public Ident Title { get; }
            public ImmutableArray<Stmt> Body { get; }
        }
        readonly record struct FuncDecl(SemanticMetadata Meta, FuncSig Sig) : Decl { }
        readonly record struct FuncDef : Decl
        {
            public FuncDef(SemanticMetadata meta, FuncSig sig, ImmutableArray<Stmt> body)
            {
                Meta = meta;
                Sig = sig;
                Debug.Assert(body.Length > 0);
                Body = body;
            }
            public SemanticMetadata Meta { get; }
            public FuncSig Sig { get; }
            public ImmutableArray<Stmt> Body { get; }
        }
    }
    interface CompilerDirective : SemanticNode, Decl, Stmt
    {
        readonly record struct Assert(SemanticMetadata Meta, Expr Expr, Option<Expr> Messsage) : CompilerDirective { }
        readonly record struct EvalExpr(SemanticMetadata Meta, Expr Expr) : CompilerDirective { }
        readonly record struct EvalType(SemanticMetadata Meta, Type Type) : CompilerDirective { }
    }
    readonly record struct Nop(SemanticMetadata Meta) : SemanticNode, Decl, Stmt { }
    interface Type : SemanticNode
    {
        readonly record struct Aliased(SemanticMetadata Meta, Ident Name) : Type { }
        readonly record struct Boolean(SemanticMetadata Meta) : Type { }
        readonly record struct Character(SemanticMetadata Meta) : Type { }
        readonly record struct Integer(SemanticMetadata Meta) : Type { }
        readonly record struct Real(SemanticMetadata Meta) : Type { }
        readonly record struct String(SemanticMetadata Meta, Option<Expr> Length) : Type { }
        readonly record struct Array(SemanticMetadata Meta, Type ItemType, ImmutableArray<Expr> Dimensions) : Type { }
        readonly record struct Structure : Type
        {
            public Structure(SemanticMetadata meta, ImmutableArray<Structure.Member> members)
            {
                Meta = meta;
                Debug.Assert(members.Length > 0);
                Members = members;
            }
            public SemanticMetadata Meta { get; }
            public ImmutableArray<Member> Members { get; }
            public interface Member
            {
                readonly record struct Component(SemanticMetadata Meta, Type Type, ImmutableArray<Ident> Names) : Member, Designator { }
            }
        }
    }
    readonly record struct InitDeclarator(SemanticMetadata Meta, Ident Name, Init Init) : SemanticNode { }
    interface Stmt : SemanticNode
    {
        readonly record struct Block : Stmt
        {
            public Block(SemanticMetadata meta, ImmutableArray<Stmt> stmts)
            {
                Meta = meta;
                Debug.Assert(stmts.Length > 0);
                Stmts = stmts;
            }
            public SemanticMetadata Meta { get; }
            public ImmutableArray<Stmt> Stmts { get; }
        }
        readonly record struct Assignment(SemanticMetadata Meta, Expr.Lvalue Target, Expr Value) : Stmt { }
        readonly record struct WhileLoop : Stmt
        {
            public WhileLoop(SemanticMetadata meta, Expr condition, ImmutableArray<Stmt> body)
            {
                Meta = meta;
                Condition = condition;
                Debug.Assert(body.Length > 0);
                Body = body;
            }
            public SemanticMetadata Meta { get; }
            public Expr Condition { get; }
            public ImmutableArray<Stmt> Body { get; }
        }
        readonly record struct DoWhileLoop : Stmt
        {
            public DoWhileLoop(SemanticMetadata meta, Expr condition, ImmutableArray<Stmt> body)
            {
                Meta = meta;
                Condition = condition;
                Debug.Assert(body.Length > 0);
                Body = body;
            }
            public SemanticMetadata Meta { get; }
            public Expr Condition { get; }
            public ImmutableArray<Stmt> Body { get; }
        }
        readonly record struct ForLoop : Stmt
        {
            public ForLoop(
                SemanticMetadata meta,
                Option<Stmt> initialization,
                Option<Expr> condition,
                Option<Stmt.Assignment> increment,
                ImmutableArray<Stmt> body
            )
            {
                Meta = meta;
                Debug.Assert(!initialization.HasValue);
                Initialization = initialization;
                Debug.Assert(!condition.HasValue);
                Condition = condition;
                Debug.Assert(!increment.HasValue);
                Increment = increment;
                Debug.Assert(body.Length > 0);
                Body = body;
            }
            public SemanticMetadata Meta { get; }
            public Option<Stmt> Initialization { get; }
            public Option<Expr> Condition { get; }
            public Option<Stmt.Assignment> Increment { get; }
            public ImmutableArray<Stmt> Body { get; }
        }
        readonly record struct Return(SemanticMetadata Meta, Expr Value) : Stmt { }
        readonly record struct Write : Stmt
        {
            public Write(SemanticMetadata meta, ImmutableArray<Expr> args)
            {
                Meta = meta;
                Debug.Assert(args.Length > 0);
                Args = args;
            }
            public SemanticMetadata Meta { get; }
            public ImmutableArray<Expr> Args { get; }
        }
        readonly record struct Read(SemanticMetadata Meta, Expr.Lvalue Target) : Stmt { }
        readonly record struct Trunc(SemanticMetadata Meta, Expr Arg) : Stmt { }
        readonly record struct LocalVarDecl(SemanticMetadata Meta, Type Type, Declarator Declarators) : Stmt { }
        readonly record struct Alternative : Stmt
        {
            public Alternative(SemanticMetadata meta, Clause @if, ImmutableArray<Clause> elseIfs, Option<ImmutableArray<Stmt>> @else)
            {
                Meta = meta;
                If = @if;
                Debug.Assert(elseIfs.Length > 0);
                ElseIfs = elseIfs;
                Debug.Assert(!@else.HasValue && @else.Value.Length > 0);
                Else = @else;
            }
            public SemanticMetadata Meta { get; }
            public Clause If { get; }
            public ImmutableArray<Clause> ElseIfs { get; }
            public Option<ImmutableArray<Stmt>> Else { get; }
        }
        readonly record struct Switch : Stmt
        {
            public Switch(SemanticMetadata meta, Expr condition, ImmutableArray<Clause> cases, Option<ImmutableArray<Stmt>> @default)
            {
                Meta = meta;
                Condition = condition;
                Debug.Assert(cases.Length > 0);
                Cases = cases;
                Debug.Assert(!@default.HasValue && @default.Value.Length > 0);
                Default = @default;
            }
            public SemanticMetadata Meta { get; }
            public Expr Condition { get; }
            public ImmutableArray<Clause> Cases { get; }
            public Option<ImmutableArray<Stmt>> Default { get; }
        }
    }
    readonly record struct FuncSig : SemanticNode
    {
        public FuncSig(SemanticMetadata meta, Ident name, ImmutableArray<FormalParam> @params, Option<Type> returnType)
        {
            Meta = meta;
            Name = name;
            Debug.Assert(@params.Length > 0);
            Params = @params;
            Debug.Assert(!returnType.HasValue);
            ReturnType = returnType;
        }
        public SemanticMetadata Meta { get; }
        public Ident Name { get; }
        public ImmutableArray<FormalParam> Params { get; }
        public Option<Type> ReturnType { get; }
    }
    interface Expr : SemanticNode, Stmt, Init
    {
        readonly record struct Unary(SemanticMetadata Meta, UnaryOperator Operator, Expr Operand) : Expr { }
        readonly record struct Binary(SemanticMetadata Meta, Expr Left, BinaryOperator Operator, Expr Right) : Expr { }
        readonly record struct Call : Expr
        {
            public Call(SemanticMetadata meta, Ident callee, ImmutableArray<ActualParam> args)
            {
                Meta = meta;
                Callee = callee;
                Debug.Assert(args.Length > 0);
                Args = args;
            }
            public SemanticMetadata Meta { get; }
            public Ident Callee { get; }
            public ImmutableArray<ActualParam> Args { get; }
        }
        interface Lvalue : Expr
        {
            readonly record struct ComponentAccess(SemanticMetadata Meta, Expr Structure, Ident Name) : Lvalue { }
            readonly record struct ArraySub(SemanticMetadata Meta, Expr Array, Expr Index) : Lvalue { }
            readonly record struct VarRef(SemanticMetadata Meta, bool IsOut, Ident Name) : Lvalue { }
        }
        interface Literal : Expr
        {
            readonly record struct True(SemanticMetadata Meta) : Literal { }
            readonly record struct False(SemanticMetadata Meta) : Literal { }
            readonly record struct String(SemanticMetadata Meta) : Literal, Type { }
            readonly record struct Real(SemanticMetadata Meta) : Literal, Type { }
            readonly record struct Character(SemanticMetadata Meta) : Literal, Type { }
            readonly record struct Integer(SemanticMetadata Meta) : Literal, Type { }
        }
    }
    interface Init : SemanticNode
    {
        readonly record struct Braced : Init
        {
            public Braced(SemanticMetadata meta, ImmutableArray<Braced.Item> items)
            {
                Meta = meta;
                Debug.Assert(items.Length > 0);
                Items = items;
            }
            public SemanticMetadata Meta { get; }
            public ImmutableArray<Item> Items { get; }
            public interface Item
            {
                readonly record struct Value : Item
                {
                    public Value(SemanticMetadata meta, ImmutableArray<Designator> designator, Init init)
                    {
                        Meta = meta;
                        Debug.Assert(designator.Length > 0);
                        Designator = designator;
                        Init = init;
                    }
                    public SemanticMetadata Meta { get; }
                    public ImmutableArray<Designator> Designator { get; }
                    public Init Init { get; }
                }
            }
        }
    }
    readonly record struct Declarator(SemanticMetadata Meta, Ident Name, Init Init) : SemanticNode { }
    readonly record struct Clause : SemanticNode
    {
        public Clause(SemanticMetadata meta, Expr condition, ImmutableArray<Stmt> body)
        {
            Meta = meta;
            Condition = condition;
            Debug.Assert(body.Length > 0);
            Body = body;
        }
        public SemanticMetadata Meta { get; }
        public Expr Condition { get; }
        public ImmutableArray<Stmt> Body { get; }
    }
    readonly record struct FormalParam(SemanticMetadata Meta) : SemanticNode { }
    interface UnaryOperator : SemanticNode
    {
        readonly record struct Cast(SemanticMetadata Meta) : UnaryOperator { }
        readonly record struct Minus(SemanticMetadata Meta) : UnaryOperator { }
        readonly record struct Not(SemanticMetadata Meta) : UnaryOperator { }
        readonly record struct Plus(SemanticMetadata Meta) : UnaryOperator { }
    }
    interface BinaryOperator : SemanticNode
    {
        readonly record struct Add(SemanticMetadata Meta) : BinaryOperator { }
        readonly record struct And(SemanticMetadata Meta) : BinaryOperator { }
        readonly record struct Div(SemanticMetadata Meta) : BinaryOperator { }
        readonly record struct Eq(SemanticMetadata Meta) : BinaryOperator { }
        readonly record struct Gt(SemanticMetadata Meta) : BinaryOperator { }
        readonly record struct Ge(SemanticMetadata Meta) : BinaryOperator { }
        readonly record struct Lt(SemanticMetadata Meta) : BinaryOperator { }
        readonly record struct Le(SemanticMetadata Meta) : BinaryOperator { }
        readonly record struct Mod(SemanticMetadata Meta) : BinaryOperator { }
        readonly record struct Mult(SemanticMetadata Meta) : BinaryOperator { }
        readonly record struct Ne(SemanticMetadata Meta) : BinaryOperator { }
        readonly record struct Or(SemanticMetadata Meta) : BinaryOperator { }
        readonly record struct Sub(SemanticMetadata Meta) : BinaryOperator { }
        readonly record struct Xor(SemanticMetadata Meta) : BinaryOperator { }
    }
    readonly record struct ActualParam(SemanticMetadata Meta, bool IsOut, Expr Value) : SemanticNode { }
    interface Designator : SemanticNode
    {
        readonly record struct Component(SemanticMetadata Meta, Ident Name) : Designator { }
        readonly record struct Indice(SemanticMetadata Meta, Expr At) : Designator { }
    }
}
