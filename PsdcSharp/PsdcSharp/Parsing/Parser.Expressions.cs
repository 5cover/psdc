using Scover.Psdc.Parsing.Nodes;
using Scover.Psdc.Tokenization;
using static Scover.Psdc.Tokenization.TokenType;

namespace Scover.Psdc.Parsing;

internal partial class Parser
{
    private readonly IReadOnlyDictionary<TokenType, ParseMethod<Node.Expression.Literal>> _literalParsers;

    private static readonly IReadOnlyList<IReadOnlyDictionary<TokenType, BinaryOperator>> binaryOperations = new List<Dictionary<TokenType, BinaryOperator>> {
        new() {
            [Symbol.OperatorOr] = BinaryOperator.Or,
        },
        new() {
            [Symbol.OperatorAnd] = BinaryOperator.And,
        },
        new() {
            [Symbol.OperatorEqual] = BinaryOperator.Equal,
            [Symbol.OperatorNotEqual] = BinaryOperator.NotEqual,
        },
        new() {
            [Symbol.OperatorLessThan] = BinaryOperator.LessThan,
            [Symbol.OperatorLessThanOrEqual] = BinaryOperator.LessThanOrEqual,
            [Symbol.OperatorGreaterThan] = BinaryOperator.GreaterThan,
            [Symbol.OperatorGreaterThanOrEqual] = BinaryOperator.GreaterThanOrEqual,
        },
        new() {
            [Symbol.OperatorPlus] = BinaryOperator.Plus,
            [Symbol.OperatorMinus] = BinaryOperator.Minus,
        },
        new() {
            [Symbol.OperatorMultiply] = BinaryOperator.Multiply,
            [Symbol.OperatorDivide] = BinaryOperator.Divide,
            [Symbol.OperatorModulus] = BinaryOperator.Modulus,
        },
    };

    private const string NameUnaryOperator = "unary operator";
    private static readonly IReadOnlyDictionary<TokenType, UnaryOperator> unaryOperators = new Dictionary<TokenType, UnaryOperator>() {
        [Symbol.OperatorMinus] = UnaryOperator.Minus,
        [Symbol.OperatorNot] = UnaryOperator.Not,
        [Symbol.OperatorPlus] = UnaryOperator.Plus,
    };

    private ParseResult<Node.Expression> ParseExpression(IEnumerable<Token> tokens)
     => ParseBinaryOperation(tokens, ParseExpression7, binaryOperations[0]);

    private ParseResult<Node.Expression> ParseExpression7(IEnumerable<Token> tokens)
     => ParseBinaryOperation(tokens, ParseExpression6, binaryOperations[1]);

    private ParseResult<Node.Expression> ParseExpression6(IEnumerable<Token> tokens)
     => ParseBinaryOperation(tokens, ParseExpression5, binaryOperations[2]);

    private ParseResult<Node.Expression> ParseExpression5(IEnumerable<Token> tokens)
     => ParseBinaryOperation(tokens, ParseExpression4, binaryOperations[3]);

    private ParseResult<Node.Expression> ParseExpression4(IEnumerable<Token> tokens)
     => ParseBinaryOperation(tokens, ParseExpression3, binaryOperations[4]);

    private ParseResult<Node.Expression> ParseExpression3(IEnumerable<Token> tokens)
     => ParseBinaryOperation(tokens, ParseExpression2, binaryOperations[5]);

    private ParseResult<Node.Expression> ParseExpression2(IEnumerable<Token> tokens)
     => ParseUnaryOperation(tokens, ParseExpression1, unaryOperators);

    private ParseResult<Node.Expression> ParseExpression1(IEnumerable<Token> tokens)
     => ParseTerminalExpression(tokens)
        .Else(() => ParseArraySubscript(tokens));

    private ParseResult<Node.Expression> ParseTerminalExpression(IEnumerable<Token> tokens)
     => ParseBracketed(tokens)
     .Else(() => ParseByTokenType(tokens, _literalParsers))
     .Else(() => ParseFunctionCall(tokens))
     .Else(() => ParseIdentifier(tokens)
        .Map((t, name) => new Node.Expression.VariableReference(t, name)));

    private ParseResult<Node.Expression> ParseBracketed(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Symbol.OpenBracket)
        .Parse(out var expression, ParseExpression)
        .ParseToken(Symbol.CloseBracket)
    .MapResult(t => new Node.Expression.Bracketed(t, expression));

    private ParseResult<Node.Expression> ParseFunctionCall(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .Parse(out var name, ParseIdentifier)
        .ParseToken(Symbol.OpenBracket)
        .ParseZeroOrMoreSeparated(out var parameters, ParseEffectiveParameter, Symbol.Comma)
        .ParseToken(Symbol.CloseBracket)
    .MapResult(t => new Node.Expression.FunctionCall(t, name, parameters));

    private ParseResult<Node.Expression> ParseArraySubscript(IEnumerable<Token> tokens)
     => ParseOperation.Start(this, tokens)
        .Parse(out var array, ParseTerminalExpression)
        .Parse(out var indexes, ParseIndexes)
        .MapResult(t => new Node.Expression.ArraySubscript(t, array, indexes));

    private ParseResult<IReadOnlyCollection<Node.Expression>> ParseIndexes(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Symbol.OpenSquareBracket)
        .ParseOneOrMoreSeparated(out var indexes, ParseExpression, Symbol.Comma)
        .ParseToken(Symbol.CloseSquareBracket)
    .MapResult(_ => indexes);

    private static ParseResult<Node.Expression> ParseBinaryOperation(
        IEnumerable<Token> tokens,
        Func<IEnumerable<Token>, ParseResult<Node.Expression>> descentParser,
        IReadOnlyDictionary<TokenType, BinaryOperator> operators)
    {
        var operand1 = descentParser(tokens);
        if (!operand1.HasValue) {
            return operand1;
        }

        int count = operand1.SourceTokens.Count;

        var prOperator = ParseTokenOfType(tokens.Skip(count), operators.Keys);

        while (prOperator.HasValue) {
            count += prOperator.SourceTokens.Count;

            ParseResult<Node.Expression> operand2 = descentParser(tokens.Skip(count));
            count += operand2.SourceTokens.Count;
            if (!operand2.HasValue) {
                return operand2.WithSourceTokens(new(tokens, count));
            }

            operand1 = ParseResult.Ok(new(tokens, count),
                new Node.Expression.OperationBinary(operand1.SourceTokens, operand1.Value, operators[prOperator.Value.Type], operand2.Value));
            if (!operand1.HasValue) {
                return operand1.WithSourceTokens(new(tokens, count));
            }

            prOperator = ParseTokenOfType(tokens.Skip(count), operators.Keys);
        }

        return operand1;
    }

    private static ParseResult<Node.Expression> ParseUnaryOperation(
        IEnumerable<Token> tokens,
        Func<IEnumerable<Token>, ParseResult<Node.Expression>> descentParser,
        IReadOnlyDictionary<TokenType, UnaryOperator> operators)
    {
        var prOperator = ParseTokenOfType(tokens, operators.Keys);

        return prOperator.Match(
            some: op => {
                var prExpr = descentParser(tokens.Skip(1)).Map((tokens, expr) => new Node.Expression.OperationUnary(tokens, operators[op.Type], expr));
                return prExpr.WithSourceTokens(new(tokens, prExpr.SourceTokens.Count + 1));
            },
            none: _ => descentParser(tokens));
    }
}
