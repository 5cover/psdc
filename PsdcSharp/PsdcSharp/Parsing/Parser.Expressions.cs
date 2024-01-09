using Scover.Psdc.Parsing.Nodes;
using Scover.Psdc.Tokenization;

namespace Scover.Psdc.Parsing;

internal partial class Parser
{
    private static readonly IReadOnlyDictionary<TokenType, ParseMethod<Node.Expression.Literal>> literalParsers = new Dictionary<TokenType, ParseMethod<Node.Expression.Literal>> {
        [TokenType.KeywordFalse] = tokens => ParseToken(tokens, TokenType.KeywordFalse, () => new Node.Expression.Literal.False()),
        [TokenType.KeywordTrue] = tokens => ParseToken(tokens, TokenType.KeywordTrue, () => new Node.Expression.Literal.True()),
        [TokenType.LiteralCharacter] = tokens => ParseTokenValue(tokens, TokenType.LiteralCharacter, value => new Node.Expression.Literal.Character(value)),
        [TokenType.LiteralInteger] = tokens => ParseTokenValue(tokens, TokenType.LiteralInteger, value => new Node.Expression.Literal.Integer(value)),
        [TokenType.LiteralReal] = tokens => ParseTokenValue(tokens, TokenType.LiteralReal, value => new Node.Expression.Literal.Real(value)),
        [TokenType.LiteralString] = tokens => ParseTokenValue(tokens, TokenType.LiteralString, value => new Node.Expression.Literal.String(value)),
    };

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

    private static ParseResult<Node.Expression> ParseExpression(IEnumerable<Token> tokens)
     => ParseBinaryOperation(tokens, ParseExpression7, operations[0]);

    private static ParseResult<Node.Expression> ParseExpression7(IEnumerable<Token> tokens)
     => ParseBinaryOperation(tokens, ParseExpression6, operations[1]);

    private static ParseResult<Node.Expression> ParseExpression6(IEnumerable<Token> tokens)
     => ParseBinaryOperation(tokens, ParseExpression5, operations[2]);

    private static ParseResult<Node.Expression> ParseExpression5(IEnumerable<Token> tokens)
     => ParseBinaryOperation(tokens, ParseExpression4, operations[3]);

    private static ParseResult<Node.Expression> ParseExpression4(IEnumerable<Token> tokens)
     => ParseBinaryOperation(tokens, ParseExpression3, operations[4]);

    private static ParseResult<Node.Expression> ParseExpression3(IEnumerable<Token> tokens)
     => ParseBinaryOperation(tokens, ParseExpression2, operations[5]);

    private static ParseResult<Node.Expression> ParseExpression2(IEnumerable<Token> tokens)
     => ParseUnaryOperation(tokens, ParseExpression1, operations[6]);

    private static ParseResult<Node.Expression> ParseExpression1(IEnumerable<Token> tokens)
     => ParseBracketed(tokens)
        .Else(() => ParseArraySubscript(tokens))
        .Else(() => ParseLiteral(tokens))
        .Else(() => ParseTokenValue(tokens, TokenType.Identifier)
            .Map(s => new Node.Expression.Variable(s)));

    private static ParseResult<Node.Expression> ParseLiteral(IEnumerable<Token> tokens) => ParseEither(tokens, literalParsers);

    private static ParseResult<Node.Expression> ParseBracketed(IEnumerable<Token> tokens) => ParseOperation.Start(tokens)
        .ParseToken(TokenType.OpenBracket)
        .Parse(ParseExpression, out var expression)
        .ParseToken(TokenType.CloseBracket)
    .BuildResult(() => new Node.Expression.Bracketed(expression));

    private static ParseResult<Node.Expression> ParseArraySubscript(IEnumerable<Token> tokens)
     => ParseIndexes(ParseOperation.Start(tokens)
        .Parse(tokens => ParseBracketed(tokens)
            .Else(() => ParseLiteral(tokens)), out var array), out var indexes)
        .BuildResult(() => new Node.Expression.ArraySubscript(array, indexes));

    private static ParseResult<Node.Expression> ParseBinaryOperation(
        IEnumerable<Token> tokens,
        Func<IEnumerable<Token>, ParseResult<Node.Expression>> descentParser,
        IReadOnlySet<TokenType> operators)
    {
        ParseResult<Node.Expression> operand1 = descentParser(tokens);
        int count = operand1.SourceTokens.Count;

        ParseResult<TokenType> @operator = ParseTokenType(tokens.Skip(count), operators);
        
        while (@operator.HasValue) {
            count += @operator.SourceTokens.Count;

            ParseResult<Node.Expression> operand2 = descentParser(tokens.Skip(count));
            count += operand2.SourceTokens.Count;

            operand1 = ParseResult.Ok(Take(count, tokens), new Node.Expression.OperationBinary(operand1, @operator.Value, operand2));

            @operator = ParseTokenType(tokens.Skip(count), operators);
        }

        return operand1;
    }

    private static ParseResult<Node.Expression> ParseUnaryOperation(
        IEnumerable<Token> tokens,
        Func<IEnumerable<Token>, ParseResult<Node.Expression>> descentParser,
        IReadOnlySet<TokenType> operators)
    {
        ParseResult<TokenType> tokenType = ParseTokenType(tokens, operators);
        int count = tokenType.SourceTokens.Count;

        if (!tokenType.HasValue) {
            return descentParser(tokens);
        }

        ParseResult<Node.Expression> operand = descentParser(tokens.Skip(count));

        return ParseResult.Ok(Take(count + operand.SourceTokens.Count, tokens), new Node.Expression.OperationUnary(tokenType.Value, operand));
    }

    private static ParseOperation ParseIndexes(ParseOperation parseOperation, out IReadOnlyCollection<ParseResult<Node.Expression>> indexes) => parseOperation
        .ParseToken(TokenType.OpenSquareBracket)
        .ParseOneOrMoreDelimited(ParseExpression, TokenType.CloseSquareBracket, out indexes)
        .ParseToken(TokenType.CloseSquareBracket);
}
