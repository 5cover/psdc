using Scover.Psdc.Pseudocode;
using static Scover.Psdc.Pseudocode.Associativity;
using static Scover.Psdc.StaticAnalysis.SemanticNode;

namespace Scover.Psdc.CodeGeneration.C;

sealed class OperatorTable : CodeGeneration.OperatorTable
{
    public static OperatorTable Instance { get; } = new();

    // Precedence and associativity extracted from the official C operator precedence table
    // https://en.cppreference.com/w/c/language/operator_precedence

    public override OperatorInfo And { get; } = new(LeftToRight, 11, 2, Infix("&&"));
    public override OperatorInfo Divide { get; } = new(LeftToRight, 3, 2, Infix('/'));
    public override OperatorInfo Equal { get; } = new(LeftToRight, 7, 2, Infix("=="));
    public override OperatorInfo GreaterThan { get; } = new(LeftToRight, 6, 2, Infix('>'));
    public override OperatorInfo GreaterThanOrEqual { get; } = new(LeftToRight, 6, 2, Infix(">="));
    public override OperatorInfo LessThan { get; } = new(LeftToRight, 6, 2, Infix("<"));
    public override OperatorInfo LessThanOrEqual { get; } = new(LeftToRight, 6, 2, Infix("<="));
    public override OperatorInfo Subtract { get; } = new(LeftToRight, 4, 2, Infix('-'));
    public override OperatorInfo Mod { get; } = new(LeftToRight, 3, 2, Infix('%'));
    public override OperatorInfo Multiply { get; } = new(LeftToRight, 3, 2, Infix('*'));
    public override OperatorInfo NotEqual { get; } = new(LeftToRight, 7, 2, Infix("!="));
    public override OperatorInfo Or { get; } = new(LeftToRight, 12, 2, Infix("||"));
    public override OperatorInfo Add { get; } = new(LeftToRight, 4, 2, Infix('+'));
    public override OperatorInfo Xor { get; } = new(LeftToRight, 9, 2, Infix('^'));
    public override OperatorInfo Cast(EvaluatedType target) => new(RightToLeft, 2, 1,
        (o, typeGen, p) => p[0](o.Append(Format.Code, $"({typeGen(target)})")));
    public override OperatorInfo UnaryMinus { get; } = new(RightToLeft, 2, 2, Prefix('-'));
    public override OperatorInfo Not { get; } = new(RightToLeft, 2, 2, Prefix('!'));
    public override OperatorInfo UnaryPlus { get; } = new(RightToLeft, 2, 2, Prefix('+'));

    public override OperatorInfo ArraySubscript { get; } = new(LeftToRight, 1, 2,
        (o, _, p) => p[1](p[0](o).Append('[')).Append(']'));
    public override OperatorInfo ComponentAccess { get; } = new(LeftToRight, 1, 2,
        (o, _, p) => p[1](p[0](o).Append('.')));
    public override OperatorInfo FunctionCall { get; } = new(LeftToRight, 1, -1,
        (o, _, p) => p[0](o).Append('(').AppendJoin(", ", p.Skip(1)).Append(')'));
    public OperatorInfo Dereference { get; } = new(RightToLeft, 2, 1, Prefix('*'));
    public OperatorInfo AddressOf { get; } = new(RightToLeft, 2, 1, Prefix('&'));
    public OperatorInfo SizeOf { get; } = new(RightToLeft, 2, 1, Prefix("sizeof "));

    public override int GetPrecedence(Expression expr)
     => C.IsPointerParameter(expr)
        ? Dereference.Precedence
        : base.GetPrecedence(expr);
}
