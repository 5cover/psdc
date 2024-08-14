using System.Diagnostics.CodeAnalysis;
using Scover.Psdc.Tokenization;

using static Scover.Psdc.Parsing.Node;
using static Scover.Psdc.Tokenization.TokenType.Regular;

namespace Scover.Psdc.Parsing;

partial class Parser
{
    static readonly Dictionary<TokenType, Func<SourceTokens, BinaryOperator>>
        operatorsOr = new() {
            [Operator.Or] = t => new BinaryOperator.Or(t),
        },
        operatorsAnd = new() {
            [Operator.And] = t => new BinaryOperator.And(t),
        },
        operatorsXor = new() {
            [Operator.Xor] = t => new BinaryOperator.Xor(t),
        },
        operatorsEquality = new() {
            [Operator.DoubleEqual] = t => new BinaryOperator.Equal(t),
            [Operator.NotEqual] = t => new BinaryOperator.NotEqual(t),
        },
        operatorsComparison = new() {
            [Operator.LessThan] = t => new BinaryOperator.LessThan(t),
            [Operator.LessThanOrEqual] = t => new BinaryOperator.LessThanOrEqual(t),
            [Operator.GreaterThan] = t => new BinaryOperator.GreaterThan(t),
            [Operator.GreaterThanOrEqual] = t => new BinaryOperator.GreaterThanOrEqual(t),
        },
        operatorsAddSub = new() {
            [Operator.Plus] = t => new BinaryOperator.Add(t),
            [Operator.Minus] = t => new BinaryOperator.Subtract(t),
        },
        operatorsMulDivMod = new() {
            [Operator.Times] = t => new BinaryOperator.Multiply(t),
            [Operator.Divide] = t => new BinaryOperator.Divide(t),
            [Operator.Mod] = t => new BinaryOperator.Mod(t),
        };

    readonly IReadOnlyDictionary<TokenType, Parser<Expression>> _literalParsers;
    delegate ParseResult<TRight> RightParser<in TLeft, out TRight>(TLeft left, IEnumerable<Token> rightTokens);

    private Parser<Expression>? _expressionParser;
    ParseResult<Expression> ParseExpression(IEnumerable<Token> tokens)
     => (_expressionParser ??=
        ParserBinaryOperation(operatorsOr,
        ParserBinaryOperation(operatorsAnd,
        ParserBinaryOperation(operatorsXor,
        ParserBinaryOperation(operatorsEquality,
        ParserBinaryOperation(operatorsComparison,
        ParserBinaryOperation(operatorsAddSub,
        ParserBinaryOperation(operatorsMulDivMod,
        ParserUnaryOperation(
        ParserBinary(ParseTerminalRvalue, new() {
            [Punctuation.LBracket] = RightParseArraySubscript,
            [Operator.Dot] = RightParseComponentAccess,
        }))))))))))(tokens);

    static Parser<T> ParserBinary<T>(
        Parser<T> leftParser,
        Dictionary<TokenType, RightParser<T, T>> rightParsers)
     => tokens => {
         var result = leftParser(tokens);
         int count = result.SourceTokens.Count;

         while (result.HasValue && TryGetRightParser(out var rightParser, rightParsers, tokens, count)) {
             count++;
             result = rightParser(result.Value, tokens.Skip(count));
             count += result.SourceTokens.Count;
         }

         return result.WithSourceTokens(new(tokens, count));
     };

    static Parser<TRight> ParserBinaryAtLeast1<TLeft, TRight>(
        string production,
        Parser<TLeft> leftParser,
        Dictionary<TokenType, RightParser<TLeft, TRight>> rightParsers)
        where TRight : TLeft
     => tokens => {
         ParseResult<TLeft> leftSeed = leftParser(tokens);
         int count = leftSeed.SourceTokens.Count;

         // Since a TLeft can't be returned in place of a TRight, we don't allow only having a TLeft parsed.
         // This is different from binary operations.
         // The initial TLeft serves as a seed for the rightParsers.
         if (!leftSeed.HasValue) {
             return ParseResult.Fail<TRight>(leftSeed.SourceTokens, leftSeed.Error);
         }
         if (!TryGetRightParser(out var rightParse, rightParsers, tokens, count)) {
             return ParseResult.Fail<TRight>(leftSeed.SourceTokens,
                ParseError.ForProduction(production, tokens.ElementAtOrNone(count), rightParsers.Keys));
         }

         ParseResult<TRight> result;
         TLeft? lastValue = leftSeed.Value;
         do {
             ++count; // read right parser token
             result = rightParse(lastValue, tokens.Skip(count));
             count += result.SourceTokens.Count;
             lastValue = result.Value;
         } while (lastValue is not null && TryGetRightParser(out rightParse, rightParsers, tokens, count));

         return result.WithSourceTokens(new(tokens, count));
     };

    static Parser<Expression> ParserBinaryOperation(
        IReadOnlyDictionary<TokenType, Func<SourceTokens, BinaryOperator>> operators,
        Parser<Expression> descentParser)
     => tokens => {
         var prLeft = descentParser(tokens);
         int count = prLeft.SourceTokens.Count;

         while (prLeft.HasValue
             && GetByTokenType(tokens.Skip(count), "operator", operators)
                is { HasValue: true } prOp) {
             count += prOp.SourceTokens.Count;
             var prRright = descentParser(tokens.Skip(count));
             count += prRright.SourceTokens.Count;

             prLeft = prRright.WithSourceTokens(new(tokens, count)).Map((srcTokens, right)
              => new Expression.BinaryOperation(srcTokens, prLeft.Value, prOp.Value(prOp.SourceTokens), right));
         }

         return prLeft;
     };

    static bool TryGetRightParser<TLeft, TRight>(
        [NotNullWhen(true)] out RightParser<TLeft, TRight>? right,
        IReadOnlyDictionary<TokenType, RightParser<TLeft, TRight>> rightParsers,
        IEnumerable<Token> tokens, int count) where TRight : TLeft
    {
        if (tokens.ElementAtOrNone(count) is { HasValue: true } middle
        && rightParsers.TryGetValue(middle.Value.Type, out right)) {
            return true;
        }
        right = null;
        return false;
    }

    ParseResult<Expression> ParseBracketed(IEnumerable<Token> tokens)
     => ParseOperation.Start(_msger, tokens, "bracketed expression")
        .ParseToken(Punctuation.LParen)
        .Parse(out var expression, ParseExpression)
        .ParseToken(Punctuation.RParen)
    .MapResult(t => new Expression.Bracketed(t, expression));

    ParseResult<Expression.Lvalue> ParseBracketedLvalue(IEnumerable<Token> tokens)
     => ParseOperation.Start(_msger, tokens, "bracketed lvalue")
        .ParseToken(Punctuation.LParen)
        .Parse(out var expression, ParseLvalue)
        .ParseToken(Punctuation.RParen)
    .MapResult(t => new Expression.Lvalue.Bracketed(t, expression));

    ParseResult<Expression.BuiltinFdf> ParseBuiltinFdf(IEnumerable<Token> tokens)
     => ParseOperation.Start(_msger, tokens, "FdF call")
        .ParseToken(Keyword.Fdf)
        .ParseToken(Punctuation.LParen)
        .Parse(out var argNomLog, ParseExpression)
        .ParseToken(Punctuation.RParen)
    .MapResult(t => new Expression.BuiltinFdf(t, argNomLog));

    Parser<Expression> ParserUnaryOperation(
        Parser<Expression> descentParser)
     => tokens => {
        var prOp = ParseUnaryOperator(tokens);
        return prOp.Match(
            op => {
                var prOperand = ParserUnaryOperation(descentParser)(tokens.Skip(prOp.SourceTokens.Count));
                return prOperand.WithSourceTokens(new(tokens, prOp.SourceTokens.Count + prOperand.SourceTokens.Count))
                    .Map((t, expr) => new Expression.UnaryOperation(t, op, expr));
            },
            _ => descentParser(tokens));
     };

    private Parser<UnaryOperator>? _parseUnaryOperator;
    Parser<UnaryOperator> ParseUnaryOperator => _parseUnaryOperator ??= ParserFirst<UnaryOperator>(
        t => ParseToken(t, Operator.Minus, t => new UnaryOperator.Minus(t)),
        t => ParseToken(t, Operator.Not, t => new UnaryOperator.Not(t)),
        t => ParseToken(t, Operator.Plus, t => new UnaryOperator.Plus(t)),
        t => ParseOperation.Start(_msger, t, "cast operator")
            .ParseToken(Punctuation.LParen)
            .Parse(out var target, ParseType)
            .ParseToken(Punctuation.RParen)
        .MapResult(t => new UnaryOperator.Cast(t, target)));

    ParseResult<Expression.FunctionCall> ParseFunctionCall(IEnumerable<Token> tokens)
     => ParseOperation.Start(_msger, tokens, "function call")
        .Parse(out var name, ParseIdentifier)
        .ParseToken(Punctuation.LParen)
        .ParseZeroOrMoreSeparated(out var parameters, ParseParameterActual, Punctuation.Comma, Punctuation.RParen)
    .MapResult(t => new Expression.FunctionCall(t, name, parameters));

    ParseResult<Expression.Lvalue> ParseLvalue(IEnumerable<Token> tokens)
     => ParserFirst(ParserBinaryAtLeast1<Expression, Expression.Lvalue>("lvalue", ParseTerminalRvalue, new() {
         [Punctuation.LBracket] = RightParseArraySubscript,
         [Operator.Dot] = RightParseComponentAccess,
     }), ParseTerminalLvalue)(tokens);

    ParseResult<Expression.Lvalue> ParseTerminalLvalue(IEnumerable<Token> tokens)
     => ParserFirst(
            t => ParseIdentifier(tokens)
                .Map((t, name) => new Expression.Lvalue.VariableReference(t, name)),
            ParseBracketedLvalue)(tokens);

    ParseResult<Expression> ParseTerminalRvalue(IEnumerable<Token> tokens)
     => ParserFirst(
            t => ParseByTokenType(t, "literal", _literalParsers),
            ParseFunctionCall,
            ParseTerminalLvalue,
            ParseBracketed,
            ParseBuiltinFdf)(tokens);

    ParseResult<Expression.Lvalue> RightParseArraySubscript(
        Expression expr,
        IEnumerable<Token> rightTokens)
     => ParseOperation.Start(_msger, rightTokens, "array subscript")
        .ParseOneOrMoreSeparated(out var indexes, ParseExpression,
            Punctuation.Comma, Punctuation.RBracket)
        .MapResult(t => new Expression.Lvalue.ArraySubscript(t, expr, indexes));

    ParseResult<Expression.Lvalue> RightParseComponentAccess(
        Expression expr,
        IEnumerable<Token> rightTokens)
     => ParseOperation.Start(_msger, rightTokens, "component access")
        .Parse(out var component, ParseIdentifier)
        .MapResult(t => new Expression.Lvalue.ComponentAccess(t, expr, component));
}
