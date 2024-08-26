using Scover.Psdc.Language;
using static Scover.Psdc.Language.Associativity;
using static Scover.Psdc.StaticAnalysis.SemanticNode;

namespace Scover.Psdc.CodeGeneration.C;

sealed class OperatorTable : CodeGeneration.OperatorTable
{
    public static OperatorTable Instance { get; } = new();

    public override OperatorInfo None { get; } = new(LeftToRight, "", int.MinValue);

    // Precedence and associativity extracted from the official C operator precedence table
    // https://en.cppreference.com/w/c/language/operator_precedence

    public override OperatorInfo And { get; } = new(LeftToRight, "&&", 11);
    public override OperatorInfo Divide { get; } = new(LeftToRight, "/", 3);
    public override OperatorInfo Equal { get; } = new(LeftToRight, "==", 7);
    public override OperatorInfo GreaterThan { get; } = new(LeftToRight, ">", 6);
    public override OperatorInfo GreaterThanOrEqual { get; } = new(LeftToRight, ">=", 6);
    public override OperatorInfo LessThan { get; } = new(LeftToRight, "<", 6);
    public override OperatorInfo LessThanOrEqual { get; } = new(LeftToRight, "<=", 6);
    public override OperatorInfo Subtract { get; } = new(LeftToRight, "-", 4);
    public override OperatorInfo Mod { get; } = new(LeftToRight, "%", 3);
    public override OperatorInfo Multiply { get; } = new(LeftToRight, "*", 3);
    public override OperatorInfo NotEqual { get; } = new(LeftToRight, "!=", 7);
    public override OperatorInfo Or { get; } = new(LeftToRight, "||", 12);
    public override OperatorInfo Add { get; } = new(LeftToRight, "+", 4);
    public override OperatorInfo Xor { get; } = new(LeftToRight, "^", 9);
    public override OperatorInfo Cast(EvaluatedType target) => new(RightToLeft,
        new(typeGen => string.Create(Format.Code, $"({typeGen(target)})")), 2);
    public override OperatorInfo UnaryMinus { get; } = new(RightToLeft, "-", 2);
    public override OperatorInfo Not { get; } = new(RightToLeft, "!", 2);
    public override OperatorInfo UnaryPlus { get; } = new(RightToLeft, "+", 2);

    public override OperatorInfo ArraySubscript { get; } = new(LeftToRight, "[]", 1);
    public override OperatorInfo ComponentAccess { get; } = new(LeftToRight, ".", 1);
    public override OperatorInfo FunctionCall { get; } = new(LeftToRight, "()", 1);
    public OperatorInfo Dereference { get; } = new(RightToLeft, "*", 2);
    public OperatorInfo AddressOf { get; } = new(RightToLeft, "&", 2);

    public override int GetPrecedence(Expression expr)
     => C.IsPointerParameter(expr)
        ? Dereference.Precedence
        : base.GetPrecedence(expr);
}
