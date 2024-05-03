namespace Scover.Psdc.Parsing;

internal static class OperatorExtensions
{
    public static string GetRepresentation(this BinaryOperator @operator) => @operator switch {
        BinaryOperator.And => "ET",
        BinaryOperator.Divide => "/",
        BinaryOperator.Equal => "==",
        BinaryOperator.GreaterThan => ">",
        BinaryOperator.GreaterThanOrEqual => ">=",
        BinaryOperator.LessThan => "<",
        BinaryOperator.LessThanOrEqual => "<=",
        BinaryOperator.Minus => "-",
        BinaryOperator.Modulus => "%",
        BinaryOperator.Multiply => "*",
        BinaryOperator.NotEqual => "!=",
        BinaryOperator.Or => "OU",
        BinaryOperator.Plus => "+",
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

internal enum BinaryOperator
{
    And,
    Divide,
    Equal,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Minus,
    Modulus,
    Multiply,
    NotEqual,
    Or,
    Plus,
    Xor,
}

internal enum UnaryOperator
{
    Minus,
    Not,
    Plus,
}
