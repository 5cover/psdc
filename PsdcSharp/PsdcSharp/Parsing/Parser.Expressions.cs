
using Scover.Psdc.Tokenization;
using static Scover.Psdc.Parsing.Node;
using static Scover.Psdc.Tokenization.TokenType;

namespace Scover.Psdc.Parsing;

internal partial class Parser
{
    private readonly IReadOnlyDictionary<TokenType, Parser<Expression>> _literalParsers;

    private static readonly Dictionary<TokenType, BinaryOperator>
        operatorsOr = new() {
            [Operator.Or] = BinaryOperator.Or,
        },
        operatorsAnd = new() {
            [Operator.And] = BinaryOperator.And,
        },
        operatorsXor = new() {
            [Operator.Xor] = BinaryOperator.Xor,
        },
        operatorsEquality = new() {
            [Operator.Equal] = BinaryOperator.Equal,
            [Operator.NotEqual] = BinaryOperator.NotEqual,
        },
        operatorsComparison = new() {
            [Operator.LessThan] = BinaryOperator.LessThan,
            [Operator.LessThanOrEqual] = BinaryOperator.LessThanOrEqual,
            [Operator.GreaterThan] = BinaryOperator.GreaterThan,
            [Operator.GreaterThanOrEqual] = BinaryOperator.GreaterThanOrEqual,
        },
        operatorsAddSub = new() {
            [Operator.Plus] = BinaryOperator.Plus,
            [Operator.Minus] = BinaryOperator.Minus,
        },
        operatorsMulDivMod = new() {
            [Operator.Multiply] = BinaryOperator.Multiply,
            [Operator.Divide] = BinaryOperator.Divide,
            [Operator.Modulus] = BinaryOperator.Modulus,
        };

    private static readonly Dictionary<TokenType, UnaryOperator>
        operatorsUnary = new() {
            [Operator.Minus] = UnaryOperator.Minus,
            [Operator.Not] = UnaryOperator.Not,
            [Operator.Plus] = UnaryOperator.Plus,
        };

    private ParseResult<Expression> ParseExpression(IEnumerable<Token> tokens)
     => ParseBinaryOperation(operatorsOr,
        ParseBinaryOperation(operatorsAnd,
        ParseBinaryOperation(operatorsXor,
        ParseBinaryOperation(operatorsEquality,
        ParseBinaryOperation(operatorsComparison,
        ParseBinaryOperation(operatorsAddSub,
        ParseBinaryOperation(operatorsMulDivMod,
        ParseUnaryOperation(operatorsUnary, ParseAnyOf(ParseArraySubscriptOrComponentAccess, ParseTerminalRvalue)))))))))(tokens);

    private ParseResult<Expression.Lvalue> ParseLvalue(IEnumerable<Token> tokens)
     => ParseAnyOf(ParseArraySubscriptOrComponentAccess, ParseTerminalLvalue)(tokens);

    private ParseResult<Expression.Lvalue> ParseArraySubscriptOrComponentAccess(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .Parse(out var rvalue, ParseTerminalRvalue)
        .ChooseBranch<Expression.Lvalue>(out var branch, new() {
            [Punctuation.OpenSquareBracket] = o => {
                o.ParseOneOrMoreSeparated(out var indexes, ParseExpression, Punctuation.Comma)
                 .ParseToken(Punctuation.CloseSquareBracket);
                return t => new Expression.Lvalue.ArraySubscript(t, rvalue, indexes);
            },
            [Operator.ComponentAccess] = o => {
                o.Parse(out var component, ParseIdentifier);
                return t => new Expression.Lvalue.ComponentAccess(t, rvalue, component);
            },
        })
        .Fork(out var result, branch)
        .MapResult(result);

    private ParseResult<Expression> ParseTerminalRvalue(IEnumerable<Token> tokens)
     => ParseAnyOf(
            ParseFunctionCall, ParseBuiltinFdf,
            ParseBracketed, ParseTerminalLvalue,
            t => ParseByTokenType(t, _literalParsers))(tokens);

    private ParseResult<Expression.Lvalue> ParseTerminalLvalue(IEnumerable<Token> tokens)
     => ParseAnyOf(ParseBracketedLvalue,
            t => ParseIdentifier(tokens)
            .Map((t, name) => new Expression.Lvalue.VariableReference(t, name)))(tokens);

    private ParseResult<Expression.Lvalue> ParseBracketedLvalue(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .ParseToken(Punctuation.OpenBracket)
        .Parse(out var expression, ParseLvalue)
        .ParseToken(Punctuation.CloseBracket)
    .MapResult(t => new Expression.Lvalue.Bracketed(t, expression));

    private ParseResult<Expression> ParseBracketed(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .ParseToken(Punctuation.OpenBracket)
        .Parse(out var expression, ParseExpression)
        .ParseToken(Punctuation.CloseBracket)
    .MapResult(t => new Expression.Bracketed(t, expression));

    private ParseResult<Expression.FunctionCall> ParseFunctionCall(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .Parse(out var name, ParseIdentifier)
        .ParseToken(Punctuation.OpenBracket)
        .ParseZeroOrMoreSeparated(out var parameters, ParseParameterActual, Punctuation.Comma)
        .ParseToken(Punctuation.CloseBracket)
    .MapResult(t => new Expression.FunctionCall(t, name, parameters));

    private ParseResult<Expression.BuiltinFdf> ParseBuiltinFdf(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .ParseToken(Keyword.Fdf)
        .ParseToken(Punctuation.OpenBracket)
        .Parse(out var argNomLog, ParseExpression)
        .ParseToken(Punctuation.CloseBracket)
    .MapResult(t => new Expression.BuiltinFdf(t, argNomLog));

    private ParseResult<IReadOnlyCollection<Expression>> ParseIndexes(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .ParseToken(Punctuation.OpenSquareBracket)
        .ParseOneOrMoreSeparated(out var indexes, ParseExpression, Punctuation.Comma)
        .ParseToken(Punctuation.CloseSquareBracket)
    .MapResult(_ => indexes);

    private static Parser<Expression> ParseBinaryOperation(
        IReadOnlyDictionary<TokenType, BinaryOperator> operators,
        Parser<Expression> descentParser)
     => tokens => {
         var prOperand1 = descentParser(tokens);
         if (!prOperand1.HasValue) {
             return prOperand1;
         }
         int count = prOperand1.SourceTokens.Count;
         var operand1 = prOperand1.Value;

         var prOperator = ParseTokenOfType(tokens.Skip(count), operators.Keys);

         while (prOperator.HasValue) {
             count += prOperator.SourceTokens.Count;

             ParseResult<Expression> operand2 = descentParser(tokens.Skip(count));
             count += operand2.SourceTokens.Count;
             if (!operand2.HasValue) {
                 return operand2.WithSourceTokens(new(tokens, count));
             }

             operand1 = new Expression.OperationBinary(new(tokens, count),
                 operand1, operators[prOperator.Value.Type], operand2.Value);

             prOperator = ParseTokenOfType(tokens.Skip(count), operators.Keys);
         }

         return ParseResult.Ok(new(tokens, count), operand1);
    };

    private static Parser<Expression> ParseUnaryOperation(
        IReadOnlyDictionary<TokenType, UnaryOperator> operators,
        Parser<Expression> descentParser)
     => tokens => {
         var prOperator = ParseTokenOfType(tokens, operators.Keys);

         return prOperator.Match(
             some: op => {
                 var prExpr = descentParser(tokens.Skip(1)).Map((tokens, expr)
                      => new Expression.OperationUnary(tokens, operators[op.Type], expr));
                 return prExpr.WithSourceTokens(new(tokens, prExpr.SourceTokens.Count + 1));
             },
             none: _ => descentParser(tokens));
    };
}
