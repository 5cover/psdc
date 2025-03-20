using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Scover.Psdc.Lexing;

using static Scover.Psdc.Parsing.Node;
using static Scover.Psdc.Lexing.TokenType;

namespace Scover.Psdc.Parsing;

partial class Parser
{
    static readonly Dictionary<TokenType, ResultCreator<BinaryOperator>>
        operatorsOr = new() {
            [Keyword.Or] = t => new BinaryOperator.Or(t),
        },
        operatorsAnd = new() {
            [Keyword.And] = t => new BinaryOperator.And(t),
        },
        operatorsXor = new() {
            [Keyword.Xor] = t => new BinaryOperator.Xor(t),
        },
        operatorsEquality = new() {
            [Punctuation.DoubleEqual] = t => new BinaryOperator.Equal(t),
            [Punctuation.NotEqual] = t => new BinaryOperator.NotEqual(t),
        },
        operatorsComparison = new() {
            [Punctuation.LessThan] = t => new BinaryOperator.LessThan(t),
            [Punctuation.LessThanOrEqual] = t => new BinaryOperator.LessThanOrEqual(t),
            [Punctuation.GreaterThan] = t => new BinaryOperator.GreaterThan(t),
            [Punctuation.GreaterThanOrEqual] = t => new BinaryOperator.GreaterThanOrEqual(t),
        },
        operatorsAddSub = new() {
            [Punctuation.Plus] = t => new BinaryOperator.Add(t),
            [Punctuation.Minus] = t => new BinaryOperator.Subtract(t),
        },
        operatorsMulDivMod = new() {
            [Punctuation.Times] = t => new BinaryOperator.Multiply(t),
            [Punctuation.Divide] = t => new BinaryOperator.Divide(t),
            [Punctuation.Mod] = t => new BinaryOperator.Mod(t),
        };

    readonly Parser<Expr.Literal> _literal;
    delegate ParseResult<TRight> RightParser<in TLeft, out TRight>(TLeft left, IEnumerable<Token> rightTokens);

    Parser<Expr>? _expressionParser;
    ParseResult<Expr> Expression(IEnumerable<Token> tokens)
     => (_expressionParser ??=
        ParserBinaryOperation(operatorsOr,
        ParserBinaryOperation(operatorsAnd,
        ParserBinaryOperation(operatorsXor,
        ParserBinaryOperation(operatorsEquality,
        ParserBinaryOperation(operatorsComparison,
        ParserBinaryOperation(operatorsAddSub,
        ParserBinaryOperation(operatorsMulDivMod,
        ParserUnaryOperation(
        ParserBinary(TerminalRvalue, new() {
            [Punctuation.LBracket] = ArraySubscriptRight,
            [Punctuation.Dot] = ComponentAccessRight,
        }))))))))))(tokens);

    static Parser<T> ParserBinary<T>(
        Parser<T> leftParser,
        Dictionary<TokenType, RightParser<T, T>> rightParsers)
     => tokens => {
         var result = leftParser(tokens);
         int count = result.SourceTokens.Count;

         while (result.HasValue && TryGetRightParser(out var rightParser, rightParsers, tokens, count)) {
             ++count;
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
                ParseError.ForProduction(production, tokens.ElementAtOrNone(count), rightParsers.Keys.ToImmutableHashSet()));
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

    static Parser<Expr> ParserBinaryOperation(
        IReadOnlyDictionary<TokenType, ResultCreator<BinaryOperator>> operators,
        Parser<Expr> descentParser)
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
                 // ReSharper disable once AccessToModifiedClosure
                 // Justification: the closure is executed immediately
                 => new Expr.BinaryOperation(srcTokens.Location, prLeft.Value, prOp.Value(prOp.SourceTokens.Location), right));
         }

         return prLeft;
     };

    static bool TryGetRightParser<TLeft, TRight>(
        [NotNullWhen(true)] out RightParser<TLeft, TRight>? right,
        IReadOnlyDictionary<TokenType, RightParser<TLeft, TRight>> rightParsers,
        IEnumerable<Token> tokens, int count)
    {
        if (tokens.ElementAtOrNone(count) is { HasValue: true } middle
        && rightParsers.TryGetValue(middle.Value.Type, out right)) {
            return true;
        }
        right = null;
        return false;
    }

    ParseResult<Expr> ParenExpr(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "parenthesized expression")
        .ParseToken(Punctuation.LParen)
        .Parse(out var expression, Expression)
        .ParseToken(Punctuation.RParen)
    .MapResult(t => new Expr.ParenExprImpl(t, expression));

    ParseResult<Expr.Lvalue> ParenLvalue(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "parenthesized lvalue")
        .ParseToken(Punctuation.LParen)
        .Parse(out var expression, ParseLvalue)
        .ParseToken(Punctuation.RParen)
    .MapResult(t => new Expr.Lvalue.ParenLValue(t, expression));

    ParseResult<Expr.BuiltinFdf> BuiltinFdf(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "FdF call")
        .ParseToken(Keyword.Fdf)
        .ParseToken(Punctuation.LParen)
        .Parse(out var argNomLog, Expression)
        .ParseToken(Punctuation.RParen)
    .MapResult(t => new Expr.BuiltinFdf(t, argNomLog));

    Parser<Expr> ParserUnaryOperation(
        Parser<Expr> descentParser)
     => tokens => {
         var prOp = ParseUnaryOperator(tokens);
         return prOp.Match(
             op => {
                 var prOperand = ParserUnaryOperation(descentParser)(tokens.Skip(prOp.SourceTokens.Count));
                 return prOperand.WithSourceTokens(new(tokens, prOp.SourceTokens.Count + prOperand.SourceTokens.Count))
                     .Map((t, expr) => new Expr.UnaryOperation(t.Location, op, expr));
             },
             _ => descentParser(tokens));
     };

    Parser<UnaryOperator>? _parseUnaryOperator;
    Parser<UnaryOperator> ParseUnaryOperator => _parseUnaryOperator ??= ParserFirst<UnaryOperator>(
        t => ParseToken(t, Punctuation.Minus, t => new UnaryOperator.Minus(t)),
        t => ParseToken(t, Keyword.Not, t => new UnaryOperator.Not(t)),
        t => ParseToken(t, Punctuation.Plus, t => new UnaryOperator.Plus(t)),
        t => ParseOperation.Start(t, "cast operator")
            .ParseToken(Punctuation.LParen)
            .Parse(out var target, _type)
            .ParseToken(Punctuation.RParen)
        .MapResult(t => new UnaryOperator.Cast(t, target)));

    ParseResult<Expr.Call> Call(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "call")
        .Parse(out var name, Identifier)
        .ParseToken(Punctuation.LParen)
        .ParseZeroOrMoreSeparated(out var parameters, ParameterActual, Punctuation.Comma, Punctuation.RParen)
    .MapResult(t => new Expr.Call(t, name, ReportErrors(parameters)));

    ParseResult<Expr.Lvalue> ParseLvalue(IEnumerable<Token> tokens)
     => ParserFirst(ParserBinaryAtLeast1<Expr, Expr.Lvalue>("lvalue", TerminalRvalue, new() {
         [Punctuation.LBracket] = ArraySubscriptRight,
         [Punctuation.Dot] = ComponentAccessRight,
     }), TerminalLvalue)(tokens);

    ParseResult<Expr.Lvalue> TerminalLvalue(IEnumerable<Token> tokens)
     => ParserFirst(
            t => Identifier(tokens)
                .Map((t, name) => new Expr.Lvalue.VariableReference(t.Location, name)),
            ParenLvalue)(tokens);

    ParseResult<Expr> TerminalRvalue(IEnumerable<Token> tokens)
     => ParserFirst(
            _literal,
            Call,
            TerminalLvalue,
            ParenExpr,
            BuiltinFdf)(tokens);

    ParseResult<Expr.Lvalue> ArraySubscriptRight(
        Expr expr,
        IEnumerable<Token> rightTokens)
     => ParseOperation.Start(rightTokens, "array subscript")
        .ParseOneOrMoreSeparated(out var indexes, Expression,
            Punctuation.Comma, Punctuation.RBracket)
        .MapResult(t => {
            var result = expr;
            var ind = ReportErrors(indexes).AsSpan();
            Debug.Assert(!ind.IsEmpty);
            foreach (var index in ind) {
                result = new Expr.Lvalue.ArraySubscript(index.Location, result, index);
            }
            return (Expr.Lvalue.ArraySubscript)result; // this cast is safe as ind cannot be empty.
        });

    ParseResult<Expr.Lvalue> ComponentAccessRight(
        Expr expr,
        IEnumerable<Token> rightTokens)
     => ParseOperation.Start(rightTokens, "component access")
        .Parse(out var component, Identifier)
        .MapResult(t => new Expr.Lvalue.ComponentAccess(t, expr, component));
}
