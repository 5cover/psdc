using Scover.Psdc.Language;
using static Scover.Psdc.Language.Associativity;
using static Scover.Psdc.Language.BinaryOperator;
using static Scover.Psdc.Language.UnaryOperator;

namespace Scover.Psdc.CodeGeneration.C;

internal sealed class OperatorInfoC : OperatorInfo<OperatorInfoC>
{
    private OperatorInfoC(Associativity associativity, string code, int precedence)
     => (Code, Associativity, Precedence) = (code, associativity, precedence);

    public string Code { get; }
    public Associativity Associativity { get; }
    public int Precedence { get; }

    // Precedence and associativity extracted from the official C operator precedence table
    // https://en.cppreference.com/w/c/language/operator_precedence

    public static OperatorInfoC FunctionCall { get; } = new(LeftToRight, "()", 1);
    public static OperatorInfoC ArraySubscript { get; } = new(LeftToRight, "[]", 1);
    public static OperatorInfoC ComponentAccess { get; } = new(LeftToRight, ".", 1);

    private static readonly Dictionary<BinaryOperator, OperatorInfoC> binary = new() {
            [And] = new(LeftToRight, "&&", 11),
            [Divide] = new(LeftToRight, "/", 3),
            [Equal] = new(LeftToRight, "==", 7),
            [GreaterThan] = new(LeftToRight, ">", 6),
            [GreaterThanOrEqual] = new(LeftToRight, ">=", 6),
            [LessThan] = new(LeftToRight, "<", 6),
            [LessThanOrEqual] = new(LeftToRight, "<=", 6),
            [BinaryOperator.Minus] = new(LeftToRight, "-", 4),
            [Modulus] = new(LeftToRight, "%", 3),
            [Multiply] = new(LeftToRight, "*", 3),
            [NotEqual] = new(LeftToRight, "!=", 7),
            [Or] = new(LeftToRight, "||", 12),
            [BinaryOperator.Plus] = new(LeftToRight, "+", 4),
            [Xor] = new(LeftToRight, "^", 9),
    };

    private static readonly Dictionary<UnaryOperator, OperatorInfoC> unary = new() {
        [UnaryOperator.Minus] = new(RightToLeft, "-", 2),
        [Not] = new(RightToLeft, "!", 2),
        [UnaryOperator.Plus] = new(RightToLeft, "+", 2),
    };

    public static OperatorInfoC Get(BinaryOperator op) => binary[op];
    public static OperatorInfoC Get(UnaryOperator op) => unary[op];
}
