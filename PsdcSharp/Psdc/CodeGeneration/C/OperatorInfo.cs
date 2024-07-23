using Scover.Psdc.Language;

using static Scover.Psdc.Language.Associativity;
using static Scover.Psdc.Language.BinaryOperator;
using static Scover.Psdc.Language.UnaryOperator;

namespace Scover.Psdc.CodeGeneration.C;

sealed class OperatorInfo : OperatorInfo<OperatorInfo>
{
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

    OperatorInfo(Associativity associativity, string code, int precedence)
             => (Code, Associativity, Precedence) = (code, associativity, precedence);

    public static OperatorInfo ArraySubscript { get; } = new(LeftToRight, "[]", 1);
    public static OperatorInfo ComponentAccess { get; } = new(LeftToRight, ".", 1);
    public static OperatorInfo FunctionCall { get; } = new(LeftToRight, "()", 1);
    public Associativity Associativity { get; }
    public string Code { get; }
    public int Precedence { get; }

    // Precedence and associativity extracted from the official C operator precedence table
    // https://en.cppreference.com/w/c/language/operator_precedence
    public static OperatorInfo Get(BinaryOperator op) => binary[op];

    public static OperatorInfo Get(UnaryOperator op) => unary[op];
}
