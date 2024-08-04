using Scover.Psdc.Language;
using Scover.Psdc.Parsing;
using static Scover.Psdc.Language.Associativity;
using static Scover.Psdc.Language.BinaryOperator;
using static Scover.Psdc.Language.UnaryOperator;
using static Scover.Psdc.Parsing.Node;

namespace Scover.Psdc.CodeGeneration.C;

sealed class OperatorTable : CodeGeneration.OperatorTable
{
    public static OperatorTable Instance { get; } = new();
    static readonly Dictionary<BinaryOperator, OperatorInfo> binary = new() {
        [And] = new(LeftToRight, "&&", 11),
        [Divide] = new(LeftToRight, "/", 3),
        [Equal] = new(LeftToRight, "==", 7),
        [GreaterThan] = new(LeftToRight, ">", 6),
        [GreaterThanOrEqual] = new(LeftToRight, ">=", 6),
        [LessThan] = new(LeftToRight, "<", 6),
        [LessThanOrEqual] = new(LeftToRight, "<=", 6),
        [Subtract] = new(LeftToRight, "-", 4),
        [Mod] = new(LeftToRight, "%", 3),
        [Multiply] = new(LeftToRight, "*", 3),
        [NotEqual] = new(LeftToRight, "!=", 7),
        [Or] = new(LeftToRight, "||", 12),
        [Add] = new(LeftToRight, "+", 4),
        [Xor] = new(LeftToRight, "^", 9),
    };

    static readonly Dictionary<UnaryOperator, OperatorInfo> unary = new() {
        [Minus] = new(RightToLeft, "-", 2),
        [Not] = new(RightToLeft, "!", 2),
        [Plus] = new(RightToLeft, "+", 2),
    };


    public override OperatorInfo None { get; } = new(LeftToRight, "", int.MinValue);
    public override OperatorInfo ArraySubscript { get; } = new(LeftToRight, "[]", 1);
    public override OperatorInfo ComponentAccess { get; } = new(LeftToRight, ".", 1);
    public override OperatorInfo FunctionCall { get; } = new(LeftToRight, "()", 1);
    public OperatorInfo Dereference { get; } = new(RightToLeft, "*", 2);
    public OperatorInfo AddressOf { get; } = new(RightToLeft, "&", 2);

    // Precedence and associativity extracted from the official C operator precedence table
    // https://en.cppreference.com/w/c/language/operator_precedence
    public override OperatorInfo Get(BinaryOperator op) => binary[op];

    public override OperatorInfo Get(UnaryOperator op) => unary[op];

    public override int GetPrecedence(ReadOnlyScope scope, Expression expr)
     => scope.IsPointer(expr)
        ? Dereference.Precedence
        : base.GetPrecedence(scope, expr);
}
