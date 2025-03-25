using System.Collections.Immutable;
using System.Diagnostics;

namespace Scover.Psdc.Parsing;

public interface Node
{
    readonly record struct Algorithm : Node
    {
        public Algorithm(ImmutableArray<Decl> decls)
        {
            Debug.Assert(decls.Length > 0);
            Decls = decls;
        }
        public ImmutableArray<Decl> Decls { get; }
    }
    interface Decl : FixedNode
    {
        readonly record struct ConstantDef(FixedRange Extent, Type Type, ImmutableArray<InitDeclarator> Declarators) : Decl { }
        readonly record struct TypeDef(FixedRange Extent, Type Type, ImmutableArray<Ident> Names) : Decl { }
        readonly record struct MainProgram : Decl
        {
            public MainProgram(FixedRange extent, Ident title, ImmutableArray<Stmt> body)
            {
                Extent = extent;
                Title = title;
                Debug.Assert(body.Length > 0);
                Body = body;
            }
            public FixedRange Extent { get; }
            public Ident Title { get; }
            public ImmutableArray<Stmt> Body { get; }
        }
        readonly record struct FuncDecl(FixedRange Extent, FuncSig Sig) : Decl { }
        readonly record struct FuncDef : Decl
        {
            public FuncDef(FixedRange extent, FuncSig sig, ImmutableArray<Stmt> body)
            {
                Extent = extent;
                Sig = sig;
                Debug.Assert(body.Length > 0);
                Body = body;
            }
            public FixedRange Extent { get; }
            public FuncSig Sig { get; }
            public ImmutableArray<Stmt> Body { get; }
        }
    }
    interface CompilerDirective : Decl, Stmt, Type.Structure.Member
    {
        readonly record struct Assert(FixedRange Extent, Expr Expr, Option<Expr> Messsage) : CompilerDirective { }
        readonly record struct EvalExpr(FixedRange Extent, Expr Expr) : CompilerDirective { }
        readonly record struct EvalType(FixedRange Extent, Type Type) : CompilerDirective { }
    }
    readonly record struct Nop(FixedRange Extent) : Decl, Stmt { }
    interface Type : FixedNode
    {
        readonly record struct Aliased(FixedRange Extent, Ident Name) : Type { }
        readonly record struct Boolean(FixedRange Extent) : Type { }
        readonly record struct Character(FixedRange Extent) : Type { }
        readonly record struct Integer(FixedRange Extent) : Type { }
        readonly record struct Real(FixedRange Extent) : Type { }
        readonly record struct String(FixedRange Extent, Option<Expr> Length) : Type { }
        readonly record struct Array(FixedRange Extent, Type ItemType, ImmutableArray<Expr> Dimensions) : Type { }
        readonly record struct Structure : Type
        {
            public Structure(FixedRange extent, ImmutableArray<Member> members)
            {
                Extent = extent;
                Debug.Assert(members.Length > 0);
                Members = members;
            }
            public FixedRange Extent { get; }
            public ImmutableArray<Member> Members { get; }
            public interface Member : FixedNode
            {
                readonly record struct Component(FixedRange Extent, Type Type, ImmutableArray<Ident> Names) : Member, Designator { }
            }
        }
    }
    readonly record struct InitDeclarator(Ident Name, Init Init) : Node { }
    interface Stmt : FixedNode
    {
        readonly record struct Block : Stmt
        {
            public Block(FixedRange extent, ImmutableArray<Stmt> stmts)
            {
                Extent = extent;
                Debug.Assert(stmts.Length > 0);
                Stmts = stmts;
            }
            public FixedRange Extent { get; }
            public ImmutableArray<Stmt> Stmts { get; }
        }
        readonly record struct Assignment(FixedRange Extent, Expr.Lvalue Target, Expr Value) : Stmt { }
        readonly record struct WhileLoop : Stmt
        {
            public WhileLoop(FixedRange extent, Expr condition, ImmutableArray<Stmt> body)
            {
                Extent = extent;
                Condition = condition;
                Debug.Assert(body.Length > 0);
                Body = body;
            }
            public FixedRange Extent { get; }
            public Expr Condition { get; }
            public ImmutableArray<Stmt> Body { get; }
        }
        readonly record struct DoWhileLoop : Stmt
        {
            public DoWhileLoop(FixedRange extent, Expr condition, ImmutableArray<Stmt> body)
            {
                Extent = extent;
                Condition = condition;
                Debug.Assert(body.Length > 0);
                Body = body;
            }
            public FixedRange Extent { get; }
            public Expr Condition { get; }
            public ImmutableArray<Stmt> Body { get; }
        }
        readonly record struct ForLoop : Stmt
        {
            public ForLoop(FixedRange extent, Option<Stmt> initialization, Option<Expr> condition, Option<Assignment> increment, ImmutableArray<Stmt> body)
            {
                Extent = extent;
                Debug.Assert(!initialization.HasValue);
                Initialization = initialization;
                Debug.Assert(!condition.HasValue);
                Condition = condition;
                Debug.Assert(!increment.HasValue);
                Increment = increment;
                Debug.Assert(body.Length > 0);
                Body = body;
            }
            public FixedRange Extent { get; }
            public Option<Stmt> Initialization { get; }
            public Option<Expr> Condition { get; }
            public Option<Assignment> Increment { get; }
            public ImmutableArray<Stmt> Body { get; }
        }
        readonly record struct Return(FixedRange Extent, Expr Value) : Stmt { }
        readonly record struct Write : Stmt
        {
            public Write(FixedRange extent, ImmutableArray<Expr> args)
            {
                Extent = extent;
                Debug.Assert(args.Length > 0);
                Args = args;
            }
            public FixedRange Extent { get; }
            public ImmutableArray<Expr> Args { get; }
        }
        readonly record struct Read(FixedRange Extent, Expr.Lvalue Target) : Stmt { }
        readonly record struct Trunc(FixedRange Extent, Expr Arg) : Stmt { }
        readonly record struct LocalVarDecl(FixedRange Extent, Type Type, Declarator Declarators) : Stmt { }
        readonly record struct Alternative : Stmt
        {
            public Alternative(FixedRange extent, Clause @if, ImmutableArray<Clause> elseIfs, Option<ImmutableArray<Stmt>> @else)
            {
                Extent = extent;
                If = @if;
                Debug.Assert(elseIfs.Length > 0);
                ElseIfs = elseIfs;
                Debug.Assert(!@else.HasValue || @else.Value.Length > 0);
                Else = @else;
            }
            public FixedRange Extent { get; }
            public Clause If { get; }
            public ImmutableArray<Clause> ElseIfs { get; }
            public Option<ImmutableArray<Stmt>> Else { get; }
        }
        readonly record struct Switch : Stmt
        {
            public Switch(FixedRange extent, Expr condition, ImmutableArray<Clause> cases, Option<ImmutableArray<Stmt>> @default)
            {
                Extent = extent;
                Condition = condition;
                Debug.Assert(cases.Length > 0);
                Cases = cases;
                Debug.Assert(!@default.HasValue || @default.Value.Length > 0);
                Default = @default;
            }
            public FixedRange Extent { get; }
            public Expr Condition { get; }
            public ImmutableArray<Clause> Cases { get; }
            public Option<ImmutableArray<Stmt>> Default { get; }
        }
    }
    readonly record struct FuncSig : Node
    {
        public FuncSig(Ident name, ImmutableArray<FormalParam> @params, Option<Type> returnType)
        {
            Name = name;
            Debug.Assert(@params.Length > 0);
            Params = @params;
            Debug.Assert(!returnType.HasValue);
            ReturnType = returnType;
        }
        public Ident Name { get; }
        public ImmutableArray<FormalParam> Params { get; }
        public Option<Type> ReturnType { get; }
    }
    interface Expr : Stmt, Init
    {
        readonly record struct Unary(FixedRange Extent, UnaryOperator Operator, Expr Operand) : Expr { }
        readonly record struct Binary(FixedRange Extent, Expr Left, BinaryOperator Operator, Expr Right) : Expr { }
        readonly record struct Call : Expr
        {
            public Call(FixedRange extent, Ident callee, ImmutableArray<ActualParam> args)
            {
                Extent = extent;
                Callee = callee;
                Debug.Assert(args.Length > 0);
                Args = args;
            }
            public FixedRange Extent { get; }
            public Ident Callee { get; }
            public ImmutableArray<ActualParam> Args { get; }
        }
        interface Lvalue : Expr
        {
            readonly record struct ComponentAccess(FixedRange Extent, Expr ure, Ident Name) : Lvalue { }
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
    interface Init : FixedNode
    {
        readonly record struct Braced : Init
        {
            public Braced(FixedRange extent, ImmutableArray<Item> items)
            {
                Extent = extent;
                Debug.Assert(items.Length > 0);
                Items = items;
            }
            public FixedRange Extent { get; }
            public ImmutableArray<Item> Items { get; }
            public interface Item : Node
            {
                readonly record struct Value : Item
                {
                    public Value(ImmutableArray<Designator> designator, Init init)
                    {
                        Debug.Assert(designator.Length > 0);
                        Designator = designator;
                        Init = init;
                    }
                    public ImmutableArray<Designator> Designator { get; }
                    public Init Init { get; }
                }
            }
        }
    }
    readonly record struct Declarator(Ident Name, Init Init) : Node { }
    readonly record struct Clause : Node
    {
        public Clause(Expr condition, ImmutableArray<Stmt> body)
        {
            Condition = condition;
            Debug.Assert(body.Length > 0);
            Body = body;
        }
        public Expr Condition { get; }
        public ImmutableArray<Stmt> Body { get; }
    }
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
