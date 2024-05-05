using System.Diagnostics.CodeAnalysis;

using Scover.Psdc.Language;
using Scover.Psdc.Tokenization;

using static Scover.Psdc.Parsing.Node;
using static Scover.Psdc.Tokenization.TokenType;

namespace Scover.Psdc.Parsing;

partial class Parser
{
    static readonly Dictionary<TokenType, BinaryOperator>
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
            [Operator.Add] = BinaryOperator.Add,
            [Operator.Subtract] = BinaryOperator.Subtract,
        },
        operatorsMulDivMod = new() {
            [Operator.Multiply] = BinaryOperator.Multiply,
            [Operator.Divide] = BinaryOperator.Divide,
            [Operator.Mod] = BinaryOperator.Mod,
        };

    static readonly Dictionary<TokenType, UnaryOperator>
        operatorsUnary = new() {
            [Operator.Subtract] = UnaryOperator.Minus,
            [Operator.Not] = UnaryOperator.Not,
            [Operator.Add] = UnaryOperator.Plus,
        };

    readonly IReadOnlyDictionary<TokenType, Parser<Expression>> _literalParsers;
    delegate ParseResult<TRight> RightParser<in TLeft, out TRight>(TLeft left, IEnumerable<Token> rightTokens);

    static Parser<T> ParseBinary<T>(
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

    static Parser<TRight> ParseBinaryAtLeast1<TLeft, TRight>(
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

    static Parser<Expression> ParseBinaryOperation(
        IReadOnlyDictionary<TokenType, BinaryOperator> operators,
        Parser<Expression> descentParser)
     => tokens => {
         var result = descentParser(tokens);
         int count = result.SourceTokens.Count;

         while (result.HasValue
             && GetByTokenType(tokens.Skip(count), "operator", operators)
                is { HasValue: true } op) {
             count += op.SourceTokens.Count;
             var right = descentParser(tokens.Skip(count));
             count += right.SourceTokens.Count;

             result = right.WithSourceTokens(new(tokens, count)).Map((srcTokens, operand2)
              => new Expression.BinaryOperation(srcTokens, result.Value, op.Value, operand2));
         }

         return result;
     };

    static Parser<Expression> ParseUnaryOperation(
        IReadOnlyDictionary<TokenType, UnaryOperator> operators,
        Parser<Expression> descentParser)
     => tokens => {
         var prOperator = ParseTokenOfType(tokens, "operator", operators.Keys);

         return prOperator.Match(
             some: op => {
                 var prExpr = descentParser(tokens.Skip(1)).Map((tokens, expr)
                      => new Expression.UnaryOperation(tokens, operators[op.Type], expr));
                 return prExpr.WithSourceTokens(new(tokens, prExpr.SourceTokens.Count + 1));
             },
             none: _ => descentParser(tokens));
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
     => ParseOperation.Start(_messenger, tokens, "bracketed expression")
        .ParseToken(Punctuation.OpenBracket)
        .Parse(out var expression, ParseExpression)
        .ParseToken(Punctuation.CloseBracket)
    .MapResult(t => new Expression.Bracketed(t, expression));

    ParseResult<Expression.Lvalue> ParseBracketedLvalue(IEnumerable<Token> tokens)
     => ParseOperation.Start(_messenger, tokens, "bracketed lvalue")
        .ParseToken(Punctuation.OpenBracket)
        .Parse(out var expression, ParseLvalue)
        .ParseToken(Punctuation.CloseBracket)
    .MapResult(t => new Expression.Lvalue.Bracketed(t, expression));

    ParseResult<Expression.BuiltinFdf> ParseBuiltinFdf(IEnumerable<Token> tokens)
     => ParseOperation.Start(_messenger, tokens, "FdF call")
        .ParseToken(Keyword.Fdf)
        .ParseToken(Punctuation.OpenBracket)
        .Parse(out var argNomLog, ParseExpression)
        .ParseToken(Punctuation.CloseBracket)
    .MapResult(t => new Expression.BuiltinFdf(t, argNomLog));

    ParseResult<Expression> ParseExpression(IEnumerable<Token> tokens)
                                         => ParseBinaryOperation(operatorsOr,
        ParseBinaryOperation(operatorsAnd,
        ParseBinaryOperation(operatorsXor,
        ParseBinaryOperation(operatorsEquality,
        ParseBinaryOperation(operatorsComparison,
        ParseBinaryOperation(operatorsAddSub,
        ParseBinaryOperation(operatorsMulDivMod,
        ParseUnaryOperation(operatorsUnary,
        ParseBinary(ParseTerminalRvalue, new() {
            [Punctuation.OpenSquareBracket] = RightParseArraySubscript,
            [Operator.ComponentAccess] = RightParseComponentAccess,
        })))))))))(tokens);

    ParseResult<Expression.FunctionCall> ParseFunctionCall(IEnumerable<Token> tokens)
     => ParseOperation.Start(_messenger, tokens, "function call")
        .Parse(out var name, ParseIdentifier)
        .ParseToken(Punctuation.OpenBracket)
        .ParseZeroOrMoreSeparated(out var parameters, ParseParameterActual, Punctuation.Comma, Punctuation.CloseBracket)
    .MapResult(t => new Expression.FunctionCall(t, name, parameters));

    ParseResult<Expression.Lvalue> ParseLvalue(IEnumerable<Token> tokens)
     => ParseFirst(ParseBinaryAtLeast1<Expression, Expression.Lvalue>("lvalue", ParseTerminalRvalue, new() {
         [Punctuation.OpenSquareBracket] = RightParseArraySubscript,
         [Operator.ComponentAccess] = RightParseComponentAccess,
     }), ParseTerminalLvalue)(tokens);

    ParseResult<Expression.Lvalue> ParseTerminalLvalue(IEnumerable<Token> tokens)
     => ParseFirst(
            t => ParseIdentifier(tokens)
                .Map((t, name) => new Expression.Lvalue.VariableReference(t, name)),
            ParseBracketedLvalue)(tokens);

    ParseResult<Expression> ParseTerminalRvalue(IEnumerable<Token> tokens)
     => ParseFirst(
            t => ParseByTokenType(t, "literal", _literalParsers),
            ParseFunctionCall,
            ParseTerminalLvalue,
            ParseBracketed,
            ParseBuiltinFdf)(tokens);

    ParseResult<Expression.Lvalue> RightParseArraySubscript(
                        Expression expr,
        IEnumerable<Token> rightTokens)
     => ParseOperation.Start(_messenger, rightTokens, "array subscript")
        .ParseOneOrMoreSeparated(out var indexes, ParseExpression,
            Punctuation.Comma, Punctuation.CloseSquareBracket)
    .MapResult(t => new Expression.Lvalue.ArraySubscript(t, expr, indexes));

    ParseResult<Expression.Lvalue> RightParseComponentAccess(
        Expression expr,
        IEnumerable<Token> rightTokens)
     => ParseOperation.Start(_messenger, rightTokens, "component access")
        .Parse(out var component, ParseIdentifier)
        .MapResult(t => new Expression.Lvalue.ComponentAccess(t, expr, component));
}
