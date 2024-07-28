using System.Diagnostics;
using System.Globalization;
using Scover.Psdc.Language;
using Scover.Psdc.Messages;
using Scover.Psdc.Parsing;

using static Scover.Psdc.Parsing.Node;

namespace Scover.Psdc.StaticAnalysis;

public sealed class SemanticAst
{
    internal SemanticAst(
        Algorithm root,
        IReadOnlyDictionary<NodeScoped, Scope> scopes,
        IDictionary<Expression, EvaluatedType> inferredTypes)
    {
        Root = root;
        Scopes = scopes;
        _inferredTypes = inferredTypes;
    }

    readonly IDictionary<Expression, EvaluatedType> _inferredTypes;
    internal IReadOnlyDictionary<Expression, EvaluatedType> InferredTypes => _inferredTypes.AsReadOnly();
    internal Algorithm Root { get; }
    internal IReadOnlyDictionary<NodeScoped, Scope> Scopes { get; }

    /// <summary>
    /// Invert an expression, assuming it is boolean.
    /// </summary>
    /// <param name="expr">The expression.</param>
    /// <returns>The inverse of <paramref name="expr"/> assuming it is a boolean expression.</returns>
    internal Expression Invert(Expression expr) => PrepareNewExpression(BooleanType.Instance, expr switch {
        Expression.Literal.True e => new Expression.Literal.False(e.SourceTokens),
        Expression.Literal.False e => new Expression.Literal.True(e.SourceTokens),
        NodeBracketedExpression e => Invert(e.ContainedExpression),
        Expression.UnaryOperation uo when uo.Operator is UnaryOperator.Not => uo.Operand,
        Expression.BinaryOperation bo => bo.Operator switch {
            BinaryOperator.Equal => new(bo.SourceTokens, bo.Left, BinaryOperator.NotEqual, bo.Right),
            BinaryOperator.NotEqual => new(bo.SourceTokens, bo.Left, BinaryOperator.Equal, bo.Right),
            BinaryOperator.GreaterThan => new(bo.SourceTokens, bo.Left, BinaryOperator.LessThanOrEqual, bo.Right),
            BinaryOperator.GreaterThanOrEqual => new(bo.SourceTokens, bo.Left, BinaryOperator.LessThan, bo.Right),
            BinaryOperator.LessThan => new(bo.SourceTokens, bo.Left, BinaryOperator.GreaterThanOrEqual, bo.Right),
            BinaryOperator.LessThanOrEqual => new(bo.SourceTokens, bo.Left, BinaryOperator.GreaterThan, bo.Right),
            // NOT (A AND B) <=> NOT A OR NOT B
            BinaryOperator.And => new(bo.SourceTokens, Invert(bo.Left), BinaryOperator.Or, Invert(bo.Right)),
            // NOT (A OR B) <=> NOT A AND NOT B
            BinaryOperator.Or => new(bo.SourceTokens, Invert(bo.Left), BinaryOperator.And, Invert(bo.Right)),
            // NOT (A XOR B) <=> A = B
            BinaryOperator.Xor => new(bo.SourceTokens, bo.Left, BinaryOperator.Equal, bo.Right),
            _ => bo
        },
        _ => new Expression.UnaryOperation(SourceTokens.Empty, UnaryOperator.Not, expr),
    });

    internal Option<Expression.Literal> MakeLiteral(Value value)
     => (value switch {
         BooleanValue b => b.Value.Comptime.Map(b => b
          ? (Expression.Literal)new Expression.Literal.True(SourceTokens.Empty)
          : new Expression.Literal.False(SourceTokens.Empty)),
         IntegerValue i => i.Value.Comptime.Map(i => new Expression.Literal.Integer(SourceTokens.Empty, i)),
         RealValue r => r.Value.Comptime.Map(r => new Expression.Literal.Real(SourceTokens.Empty, r)),
         StringValue s => s.Value.Comptime.Map(s => new Expression.Literal.String(SourceTokens.Empty, s)),
         _ => Option.None<Expression.Literal>(),
     }).Map(PrepareNewLiteral);

    /// <summary>
    /// Wrap an expression in a binary integer sum operation.
    /// </summary>
    /// <param name="expr">The left operand of the binary expression.</param>
    /// <param name="operator">The operator of the binary expression.</param>
    /// <param name="value">The right operand of the binary expression.</param>
    /// <param name="inferredType">The right operand of the binary expression.</param>
    /// <returns>An binary operation expression node with empty source tokens, or a simple literal expression if the operation could be eagerly evaluated, and the messages that occured during the computation. Returns <paramref name="expr"/> and an error if <paramref name="expr"/>'s type does not evaluate to integer.</returns>
    internal (Expression expression, IEnumerable<Message> messages) Alter(
        Expression expr, BinaryOperator @operator, int value)
    {
        if (!_inferredTypes[expr].IsConvertibleTo(IntegerType.Instance)) {
            return (expr, Message.ErrorExpressionHasWrongType(expr.SourceTokens, IntegerType.Instance, _inferredTypes[expr]).Yield());
        }

        var opBin = PrepareNewExpression(IntegerType.Instance, MakeBinaryOperation(expr, @operator, value));

        // Collapse literals
        if (expr is Expression.Literal.Integer intLit) {
            var sum = @operator.EvaluateOperation(IntegerType.Instance.Instantiate(intLit.Value), IntegerType.Instance.Instantiate(value));
            return MakeLiteral(sum.Value).Match<Expression.Literal, (Expression, IEnumerable<Message>)>(
                lit => (lit, sum.Messages.Select(e => e.GetOperationMessage(opBin,
                    lit.ValueType, IntegerType.Instance))),
                () => (opBin, []));
        }

        return (opBin, []);
    }

    T PrepareNewExpression<T>(EvaluatedType exprType, T expr) where T : Expression
    {
        _inferredTypes.Add(expr, exprType);
        return expr;
    }

    T PrepareNewLiteral<T>(T lit) where T : Expression.Literal
    {
        _inferredTypes.Add(lit, lit.ValueType);
        return lit;
    }

    Expression.BinaryOperation MakeBinaryOperation(Expression expr, BinaryOperator @operator, int value)
     => new(SourceTokens.Empty, expr, @operator,
            PrepareNewLiteral(new Expression.Literal.Integer(
                SourceTokens.Empty, value.ToString(CultureInfo.InvariantCulture))));

}
