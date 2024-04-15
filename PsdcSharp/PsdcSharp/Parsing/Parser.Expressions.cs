using Scover.Psdc.Parsing.Nodes;
using Scover.Psdc.Tokenization;

namespace Scover.Psdc.Parsing;

internal partial class Parser
{
    private readonly IReadOnlyDictionary<TokenType, ParseMethod<Node.Expression.Literal>> _literalParsers;

    private static readonly IReadOnlyList<IReadOnlyDictionary<TokenType, BinaryOperator>> binaryOperations = new List<Dictionary<TokenType, BinaryOperator>> {
        new() {
            [TokenType.OperatorOr] = BinaryOperator.Or,
        },
        new() {
            [TokenType.OperatorAnd] = BinaryOperator.And,
        },
        new() {
            [TokenType.OperatorEqual] = BinaryOperator.Equal,
            [TokenType.OperatorNotEqual] = BinaryOperator.NotEqual,
        },
        new() {
            [TokenType.OperatorLessThan] = BinaryOperator.LessThan,
            [TokenType.OperatorLessThanOrEqual] = BinaryOperator.LessThanOrEqual,
            [TokenType.OperatorGreaterThan] = BinaryOperator.GreaterThan,
            [TokenType.OperatorGreaterThanOrEqual] = BinaryOperator.GreaterThanOrEqual,
        },
        new() {
            [TokenType.OperatorPlus] = BinaryOperator.Plus,
            [TokenType.OperatorMinus] = BinaryOperator.Minus,
        },
        new() {
            [TokenType.OperatorMultiply] = BinaryOperator.Multiply,
            [TokenType.OperatorDivide] = BinaryOperator.Divide,
            [TokenType.OperatorModulus] = BinaryOperator.Modulus,
        },
    };

    private static readonly IReadOnlyDictionary<TokenType, UnaryOperator> unaryOperators = new Dictionary<TokenType, UnaryOperator>()
    {
        [TokenType.OperatorMinus] = UnaryOperator.Minus,
        [TokenType.OperatorNot] = UnaryOperator.Not,
        [TokenType.OperatorPlus] = UnaryOperator.Plus,
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
     => ParseBracketedOrCall(tokens)
        .Else(() => ParseArraySubscript(tokens))
        .Else(() => ParseLiteral(tokens))
        .Else(() => ParseTokenValue(tokens, TokenType.Identifier)
            .Map(s => new Node.Expression.Variable(s)));

    private ParseResult<Node.Expression> ParseLiteral(IEnumerable<Token> tokens) => ParseByTokenType(tokens, _literalParsers);

    private ParseResult<Node.Expression> ParseBracketedOrCall(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(TokenType.OpenBracket)
        .Parse(out var expression, ParseExpression)
        .ParseToken(TokenType.CloseBracket)
    .MapResult(() => new Node.Expression.Bracketed(expression));

    private ParseResult<Node.Expression> ParseArraySubscript(IEnumerable<Token> tokens)
     => ParseOperation.Start(this, tokens)
        .Parse(out var array, tokens => ParseBracketedOrCall(tokens)
                            .Else(() => ParseLiteral(tokens)))
        .Parse(out var indexes, ParseIndexes)
        .MapResult(() => new Node.Expression.ArraySubscript(array, indexes));

    private ParseResult<IReadOnlyCollection<Node.Expression>> ParseIndexes(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(TokenType.OpenSquareBracket)
        .ParseOneOrMoreSeparated(out var indexes, ParseExpression, TokenType.PunctuationComma)
        .ParseToken(TokenType.CloseSquareBracket)
    .MapResult(() => indexes);

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
                new Node.Expression.OperationBinary(operand1.Value, operators[prOperator.Value.Type], operand2.Value));
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
                var prExpr = descentParser(tokens.Skip(1)).Map(expr => new Node.Expression.OperationUnary(operators[op.Type], expr));
                return prExpr.WithSourceTokens(new(tokens, prExpr.SourceTokens.Count + 1));
            },
            none: _ => descentParser(tokens));
    }
}
