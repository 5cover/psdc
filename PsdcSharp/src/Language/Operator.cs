namespace Scover.Psdc.Language;

enum BinaryOperator
{
    And,
    Divide,
    Equal,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Subtract,
    Mod,
    Multiply,
    NotEqual,
    Or,
    Add,
    Xor,
}

static class OperatorExtensions
{
    public static Associativity GetAssociativity(this BinaryOperator @operator) => Associativity.LeftToRight;

    public static Associativity GetAssociativity(this UnaryOperator @operator) => Associativity.RightToLeft;

    public static string GetRepresentation(this BinaryOperator @operator) => @operator switch {
        BinaryOperator.And => "ET",
        BinaryOperator.Divide => "/",
        BinaryOperator.Equal => "==",
        BinaryOperator.GreaterThan => ">",
        BinaryOperator.GreaterThanOrEqual => ">=",
        BinaryOperator.LessThan => "<",
        BinaryOperator.LessThanOrEqual => "<=",
        BinaryOperator.Subtract => "-",
        BinaryOperator.Mod => "%",
        BinaryOperator.Multiply => "*",
        BinaryOperator.NotEqual => "!=",
        BinaryOperator.Or => "OU",
        BinaryOperator.Add => "+",
        BinaryOperator.Xor => "XOR",
        _ => throw @operator.ToUnmatchedException()
    };

    public static string GetRepresentation(this UnaryOperator @operator) => @operator switch {
        UnaryOperator.Minus => "-",
        UnaryOperator.Not => "NON",
        UnaryOperator.Plus => "+",
        _ => throw @operator.ToUnmatchedException()
    };
}

enum UnaryOperator
{
    Minus,
    Not,
    Plus,
}
