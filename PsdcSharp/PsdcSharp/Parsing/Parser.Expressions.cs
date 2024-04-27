using Scover.Psdc.Parsing.Nodes;
using Scover.Psdc.Tokenization;
using static Scover.Psdc.Tokenization.TokenType;

namespace Scover.Psdc.Parsing;

internal partial class Parser
{
    private readonly IReadOnlyDictionary<TokenType, ParseMethod<Node.Expression.Literal>> _literalParsers;

    private static readonly IReadOnlyList<IReadOnlyDictionary<TokenType, BinaryOperator>> binaryOperations = new List<Dictionary<TokenType, BinaryOperator>> {
        new() {
            [Operator.Or] = BinaryOperator.Or,
        },
        new() {
            [Operator.And] = BinaryOperator.And,
        },
        new() {
            [Operator.Equal] = BinaryOperator.Equal,
            [Operator.NotEqual] = BinaryOperator.NotEqual,
        },
        new() {
            [Operator.LessThan] = BinaryOperator.LessThan,
            [Operator.LessThanOrEqual] = BinaryOperator.LessThanOrEqual,
            [Operator.GreaterThan] = BinaryOperator.GreaterThan,
            [Operator.GreaterThanOrEqual] = BinaryOperator.GreaterThanOrEqual,
        },
        new() {
            [Operator.Plus] = BinaryOperator.Plus,
            [Operator.Minus] = BinaryOperator.Minus,
        },
        new() {
            [Operator.Multiply] = BinaryOperator.Multiply,
            [Operator.Divide] = BinaryOperator.Divide,
            [Operator.Modulus] = BinaryOperator.Modulus,
        },
    };

    private const string NameUnaryOperator = "unary operator";
    private static readonly IReadOnlyDictionary<TokenType, UnaryOperator> unaryOperators = new Dictionary<TokenType, UnaryOperator>() {
        [Operator.Minus] = UnaryOperator.Minus,
        [Operator.Not] = UnaryOperator.Not,
        [Operator.Plus] = UnaryOperator.Plus,
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
     => ParseAnyOf(tokens, ParseArraySubscriptOrComponentAccess, ParseTerminalRValue);

    private ParseResult<Node.Expression.LValue> ParseLValue(IEnumerable<Token> tokens)
     => ParseAnyOf(tokens, ParseArraySubscriptOrComponentAccess, ParseTerminalLValue);

    private ParseResult<Node.Expression.LValue> ParseArraySubscriptOrComponentAccess(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .Parse(out var rvalue, ParseTerminalRValue)
        .Branch<Node.Expression.LValue>(new() {
            [Punctuation.OpenSquareBracket] = o => o
                .ParseOneOrMoreSeparated(out var indexes, ParseExpression, Punctuation.Comma)
                .ParseToken(Punctuation.CloseSquareBracket)
            .MapResult(t => new Node.Expression.LValue.ArraySubscript(t, rvalue, indexes)),
            [Operator.ComponentAccess] = o => o
                .Parse(out var component, ParseIdentifier)
            .MapResult(t => new Node.Expression.LValue.ComponentAccess(t, rvalue, component)),
        });

    private ParseResult<Node.Expression> ParseTerminalRValue(IEnumerable<Token> tokens)
     => ParseAnyOf(tokens,
            ParseFunctionCall,
            ParseBuiltinFdf,
            ParseBracketed,
            ParseTerminalLValue,
            t => ParseByTokenType(t, _literalParsers));

    private ParseResult<Node.Expression.LValue> ParseTerminalLValue(IEnumerable<Token> tokens)
     => ParseAnyOf(tokens, ParseBracketedLValue,
        t => ParseIdentifier(tokens).Map((t, name) => new Node.Expression.LValue.VariableReference(t, name)));

    private ParseResult<Node.Expression.LValue> ParseBracketedLValue(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .ParseToken(Punctuation.OpenBracket)
        .Parse(out var expression, ParseLValue)
        .ParseToken(Punctuation.CloseBracket)
    .MapResult(t => new Node.Expression.LValue.Bracketed(t, expression));

    private ParseResult<Node.Expression> ParseBracketed(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .ParseToken(Punctuation.OpenBracket)
        .Parse(out var expression, ParseExpression)
        .ParseToken(Punctuation.CloseBracket)
    .MapResult(t => new Node.Expression.Bracketed(t, expression));

    private ParseResult<Node.Expression.FunctionCall> ParseFunctionCall(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .Parse(out var name, ParseIdentifier)
        .ParseToken(Punctuation.OpenBracket)
        .ParseZeroOrMoreSeparated(out var parameters, ParseEffectiveParameter, Punctuation.Comma)
        .ParseToken(Punctuation.CloseBracket)
    .MapResult(t => new Node.Expression.FunctionCall(t, name, parameters));

    private ParseResult<Node.Expression.BuiltinFdf> ParseBuiltinFdf(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .ParseToken(Keyword.Fdf)
        .ParseToken(Punctuation.OpenBracket)
        .Parse(out var argNomLog, ParseExpression)
        .ParseToken(Punctuation.CloseBracket)
    .MapResult(t => new Node.Expression.BuiltinFdf(t, argNomLog));

    private ParseResult<IReadOnlyCollection<Node.Expression>> ParseIndexes(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .ParseToken(Punctuation.OpenSquareBracket)
        .ParseOneOrMoreSeparated(out var indexes, ParseExpression, Punctuation.Comma)
        .ParseToken(Punctuation.CloseSquareBracket)
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
