using Scover.Psdc.Language;

using static Scover.Psdc.Language.Associativity;
using static Scover.Psdc.Language.BinaryOperator;
using static Scover.Psdc.Language.UnaryOperator;

namespace Scover.Psdc.CodeGeneration.C;

sealed class OperatorInfoC : OperatorInfo<OperatorInfoC>
{
    static readonly Dictionary<BinaryOperator, OperatorInfoC> binary = new() {
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

    static readonly Dictionary<UnaryOperator, OperatorInfoC> unary = new() {
        [Minus] = new(RightToLeft, "-", 2),
        [Not] = new(RightToLeft, "!", 2),
        [Plus] = new(RightToLeft, "+", 2),
    };

    OperatorInfoC(Associativity associativity, string code, int precedence)
             => (Code, Associativity, Precedence) = (code, associativity, precedence);

    public static OperatorInfoC ArraySubscript { get; } = new(LeftToRight, "[]", 1);
    public static OperatorInfoC ComponentAccess { get; } = new(LeftToRight, ".", 1);
    public static OperatorInfoC FunctionCall { get; } = new(LeftToRight, "()", 1);
    public Associativity Associativity { get; }
    public string Code { get; }
    public int Precedence { get; }

    // Precedence and associativity extracted from the official C operator precedence table
    // https://en.cppreference.com/w/c/language/operator_precedence
    public static OperatorInfoC Get(BinaryOperator op) => binary[op];

    public static OperatorInfoC Get(UnaryOperator op) => unary[op];
}
