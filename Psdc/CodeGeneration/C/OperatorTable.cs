using Scover.Psdc.Pseudocode;

using static Scover.Psdc.Pseudocode.Associativity;
using static Scover.Psdc.StaticAnalysis.SemanticNode;

namespace Scover.Psdc.CodeGeneration.C;

sealed class OperatorTable : CodeGeneration.OperatorTable
{
    public static OperatorTable Instance { get; } = new();

    // Precedence and associativity extracted from the official C operator precedence table
    // https://en.cppreference.com/w/c/language/operator_precedence

    public OperatorInfo AddressOf { get; } = new(RightToLeft, 2, 1, Prefix('&'));
    public OperatorInfo BitwiseAnd { get; } = new(LeftToRight, 8, 2, Infix("&"));
    public OperatorInfo BitwiseNot { get; } = new(LeftToRight, 2, 1, Prefix('~'));
    public OperatorInfo BitwiseOr { get; } = new(LeftToRight, 9, 2, Infix("|"));
    public OperatorInfo BitwiseXor { get; } = new(LeftToRight, 9, 2, Infix('^'));
    public OperatorInfo Dereference { get; } = new(RightToLeft, 2, 1, Prefix('*'));
    public OperatorInfo SizeOf { get; } = new(RightToLeft, 2, 1, Prefix("sizeof "));
    public override OperatorInfo Add { get; } = new(LeftToRight, 4, 2, Infix('+'));
    public override OperatorInfo And { get; } = new(LeftToRight, 11, 2, Infix("&&"));
    public override OperatorInfo ArraySubscript { get; } = new(LeftToRight, 1, 2, (o, _, p) => p[1](p[0](o).Append('[')).Append(']'));
    public override OperatorInfo Cast(EvaluatedType target) => new(RightToLeft, 2, 1, (o, typeGen, p) => p[0](o.Append(Format.Code, $"({typeGen(target)})")));
    public override OperatorInfo ComponentAccess { get; } = new(LeftToRight, 1, 2, (o, _, p) => p[1](p[0](o).Append('.')));
    public override OperatorInfo Divide { get; } = new(LeftToRight, 3, 2, Infix('/'));
    public override OperatorInfo Equal { get; } = new(LeftToRight, 7, 2, Infix("=="));
    public override OperatorInfo FunctionCall { get; } = new(LeftToRight, 1, -1, (o, _, p) => p[0](o).Append('(').AppendJoin(", ", p.Skip(1)).Append(')'));
    public override OperatorInfo GreaterThan { get; } = new(LeftToRight, 6, 2, Infix('>'));
    public override OperatorInfo GreaterThanOrEqual { get; } = new(LeftToRight, 6, 2, Infix(">="));
    public override OperatorInfo LessThan { get; } = new(LeftToRight, 6, 2, Infix("<"));
    public override OperatorInfo LessThanOrEqual { get; } = new(LeftToRight, 6, 2, Infix("<="));
    public override OperatorInfo Mod { get; } = new(LeftToRight, 3, 2, Infix('%'));
    public override OperatorInfo Multiply { get; } = new(LeftToRight, 3, 2, Infix('*'));
    public override OperatorInfo Not { get; } = new(RightToLeft, 2, 1, Prefix('!'));
    public override OperatorInfo NotEqual { get; } = new(LeftToRight, 7, 2, Infix("!="));
    public override OperatorInfo Or { get; } = new(LeftToRight, 12, 2, Infix("||"));
    public override OperatorInfo Subtract { get; } = new(LeftToRight, 4, 2, Infix('-'));
    public override OperatorInfo UnaryMinus { get; } = new(RightToLeft, 2, 1, Prefix('-'));
    public override OperatorInfo UnaryPlus { get; } = new(RightToLeft, 2, 1, Prefix('+'));

    public override int GetPrecedence(Expr expr) => C.IsPointerParameter(expr)
        ? Dereference.Precedence
        : base.GetPrecedence(expr);

    protected override OperatorInfo Get(BinaryOperator op, EvaluatedType leftType, EvaluatedType rightType)
    {
        return op switch {
            BinaryOperator.Add => Add,
            BinaryOperator.And => UseBitwise() ? BitwiseAnd : And,
            BinaryOperator.Divide => Divide,
            BinaryOperator.Equal => Equal,
            BinaryOperator.GreaterThan => GreaterThan,
            BinaryOperator.GreaterThanOrEqual => GreaterThanOrEqual,
            BinaryOperator.LessThan => LessThan,
            BinaryOperator.LessThanOrEqual => LessThanOrEqual,
            BinaryOperator.Mod => Mod,
            BinaryOperator.Multiply => Multiply,
            BinaryOperator.NotEqual => NotEqual,
            BinaryOperator.Or => UseBitwise() ? BitwiseOr : Or,
            BinaryOperator.Subtract => Subtract,
            BinaryOperator.Xor => BitwiseXor,
            _ => throw op.ToUnmatchedException(),
        };

        bool UseBitwise() => leftType.IsConvertibleTo(IntegerType.Instance) && rightType.IsConvertibleTo(IntegerType.Instance);
    }

    protected override OperatorInfo Get(UnaryOperator op, EvaluatedType operandType)
    {
        return op switch {
            UnaryOperator.Cast c => Cast(c.Target),
            UnaryOperator.Minus => UnaryMinus,
            UnaryOperator.Plus => UnaryPlus,
            UnaryOperator.Not => UseLogic() ? Not : BitwiseNot,
            _ => throw op.ToUnmatchedException(),
        };

        // Use logic if the type is unknown.
        bool UseLogic() => operandType.IsConvertibleTo(BooleanType.Instance);
    }
}
