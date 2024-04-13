using Scover.Psdc.Parsing.Nodes;
using Scover.Psdc.Tokenization;

namespace Scover.Psdc.Parsing;

internal partial class Parser
{
    private readonly IReadOnlyDictionary<TokenType, ParseMethod<Node.Expression.Literal>> _literalParsers;

    private static readonly IReadOnlyList<IReadOnlySet<TokenType>> operations = new List<HashSet<TokenType>> {
        new() {
            TokenType.OperatorOr,
        },
        new() {
            TokenType.OperatorAnd,
        },
        new() {
            TokenType.OperatorEqual,
            TokenType.OperatorNotEqual,
        },
        new() {
            TokenType.OperatorLessThan,
            TokenType.OperatorLessThanOrEqual,
            TokenType.OperatorGreaterThan,
            TokenType.OperatorGreaterThanOrEqual,
        },
        new() {
            TokenType.OperatorPlus,
            TokenType.OperatorMinus,
        },
        new() {
            TokenType.OperatorMultiply,
            TokenType.OperatorDivide,
            TokenType.OperatorModulus,
        },
        new() {
            TokenType.OperatorPlus,
            TokenType.OperatorMinus,
            TokenType.OperatorNot,
        },
    };

    private ParseResult<Node.Expression> ParseExpression(IEnumerable<Token> tokens)
     => ParseBinaryOperation(tokens, ParseExpression7, operations[0]);

    private ParseResult<Node.Expression> ParseExpression7(IEnumerable<Token> tokens)
     => ParseBinaryOperation(tokens, ParseExpression6, operations[1]);

    private ParseResult<Node.Expression> ParseExpression6(IEnumerable<Token> tokens)
     => ParseBinaryOperation(tokens, ParseExpression5, operations[2]);

    private ParseResult<Node.Expression> ParseExpression5(IEnumerable<Token> tokens)
     => ParseBinaryOperation(tokens, ParseExpression4, operations[3]);

    private ParseResult<Node.Expression> ParseExpression4(IEnumerable<Token> tokens)
     => ParseBinaryOperation(tokens, ParseExpression3, operations[4]);

    private ParseResult<Node.Expression> ParseExpression3(IEnumerable<Token> tokens)
     => ParseBinaryOperation(tokens, ParseExpression2, operations[5]);

    private ParseResult<Node.Expression> ParseExpression2(IEnumerable<Token> tokens)
     => ParseUnaryOperation(tokens, ParseExpression1, operations[6]);

    private ParseResult<Node.Expression> ParseExpression1(IEnumerable<Token> tokens)
     => ParseBracketed(tokens)
        .Else(() => ParseArraySubscript(tokens))
        .Else(() => ParseLiteral(tokens))
        .Else(() => ParseTokenValue(tokens, TokenType.Identifier)
            .Map(s => new Node.Expression.Variable(s)));

    private ParseResult<Node.Expression> ParseLiteral(IEnumerable<Token> tokens) => ParseEither(tokens, _literalParsers);

    private ParseResult<Node.Expression> ParseBracketed(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(TokenType.OpenBracket)
        .Parse(out var expression, ParseExpression)
        .ParseToken(TokenType.CloseBracket)
    .MapResult(() => new Node.Expression.Bracketed(expression));

    private ParseResult<Node.Expression> ParseArraySubscript(IEnumerable<Token> tokens)
     => ParseOperation.Start(this, tokens)
        .Parse(out var array, tokens => ParseBracketed(tokens)
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
        IReadOnlySet<TokenType> operators)
    {
        var operand1 = descentParser(tokens);
        if (!operand1.HasValue) {
            return operand1;
        }

        int count = operand1.SourceTokens.Count;

        ParseResult<TokenType> @operator = ParseTokenOfType(tokens.Skip(count), operators);
        
        while (@operator.HasValue) {
            count += @operator.SourceTokens.Count;

            ParseResult<Node.Expression> operand2 = descentParser(tokens.Skip(count));
            count += operand2.SourceTokens.Count;
            if (!operand2.HasValue) {
                return operand2.WithSourceTokens(Take(count, tokens));
            }

            operand1 = ParseResult.Ok(Take(count, tokens),
                new Node.Expression.OperationBinary(operand1.Value, @operator.Value, operand2.Value));
            if (!operand1.HasValue) {
                return operand1.WithSourceTokens(Take(count, tokens));
            }

            @operator = ParseTokenOfType(tokens.Skip(count), operators);
        }

        return operand1;
    }

    private static ParseResult<Node.Expression> ParseUnaryOperation(
        IEnumerable<Token> tokens,
        Func<IEnumerable<Token>, ParseResult<Node.Expression>> descentParser,
        IReadOnlySet<TokenType> operators)
    {
        ParseResult<TokenType> tokenType = ParseTokenOfType(tokens, operators);

        return tokenType.Match(
            some: @operator => {
                var prExpr = descentParser(tokens.Skip(1)).Map(expr => new Node.Expression.OperationUnary(@operator, expr));
                return prExpr.WithSourceTokens(Take(prExpr.SourceTokens.Count + 1, tokens));
            },
            none: _ => descentParser(tokens));
    }
}
