using Scover.Psdc.Pseudocode;

using static Scover.Psdc.StaticAnalysis.SemanticNode;
using static Scover.Psdc.Pseudocode.Associativity;
using System.Text;
using System.Diagnostics;

namespace Scover.Psdc.CodeGeneration;

delegate TypeInfo TypeGenerator(EvaluatedType type);
delegate StringBuilder OperatorInfoAppender(StringBuilder o, TypeGenerator typeGenerator, IReadOnlyList<Appender> @params);

readonly record struct OperatorInfo
{
    public OperatorInfo(
        Associativity associativity,
        int precedence,
        int arity,
        OperatorInfoAppender append) => (Associativity, _append, Arity, Precedence) = (associativity, append, arity, precedence);

    public Associativity Associativity { get; }
    private readonly OperatorInfoAppender _append;
    public StringBuilder Append(StringBuilder o, TypeGenerator typeGenerator, IReadOnlyList<Appender> @params)
    {
        Debug.Assert(Arity < 0
            ? @params.Count >= Arity * -1
            : @params.Count == Arity);
        return _append(o, typeGenerator, @params);
    }

    /// <summary>
    /// Get the arity.
    /// </summary>
    /// <remarks>Negatives value indicate a minimum arity. Example: an arity of -5 means at least 5 arguments are expected.</remarks>
    public int Arity { get; }
    public int Precedence { get; }
    public static OperatorInfo None { get; } = new(LeftToRight, int.MinValue, 0, (o, _, _) => o);
}

abstract class OperatorTable
{
    public virtual int GetPrecedence(Expression expr)
     => expr switch {
         Expression.Call or Expression.BuiltinFdf => FunctionCall.Precedence,
         Expression.BinaryOperation opBin => Get(opBin).Precedence,
         Expression.UnaryOperation opUn => Get(opUn).Precedence,
         Expression.Lvalue.ArraySubscript => ArraySubscript.Precedence,
         Expression.Lvalue.ComponentAccess => ComponentAccess.Precedence,
         Expression.Lvalue.VariableReference or Expression.Literal or BracketedExpression => int.MinValue,
         _ => throw expr.ToUnmatchedException(),
     };

    /// <summary>
    /// Should a binary operation should be bracketed.
    /// </summary>
    /// <param name="opBin">A binary operation.</param>
    /// <returns>2-uple: the left operand should be bracketed, the right operand should be bracketed.</returns>
    public (bool bracketLeft, bool bracketRight) ShouldBracketBinary(Expression.BinaryOperation opBin)
         => ShouldBracketBinary(
            GetPrecedence(opBin.Left),
            GetPrecedence(opBin.Right),
            Get(opBin.Operator, opBin.Left.Value.Type, opBin.Right.Value.Type));

    public bool ShouldBracketUnary(Expression.UnaryOperation opUn)
     => ShouldBracketOperand(Get(opUn.Operator, opUn.Operand.Value.Type), opUn.Operand);

    /// <summary>
    /// Should an component access operation's structure operand be bracketed?
    /// </summary>
    /// <param name="compAccess">A component access expression.</param>
    /// <returns><paramref name="compAccess"/>'s structure operand should be bracketed.</returns>
    public bool ShouldBracket(Expression.Lvalue.ComponentAccess compAccess)
     => ShouldBracketOperand(ComponentAccess, compAccess.Structure);

    /// <summary>
    /// Should an array subscript operation's array operand be bracketed?
    /// </summary>
    /// <param name="arrSub">An array subscript expression.</param>
    /// <returns><paramref name="arrSub"/>'s array operand should be bracketed.</returns>
    public bool ShouldBracket(Expression.Lvalue.ArraySubscript arrSub)
     => ShouldBracketOperand(ArraySubscript, arrSub.Array);

    static (bool bracketLeft, bool bracketRight) ShouldBracketBinary(int precedenceLeft, int precedenceRight, OperatorInfo op)
     => (bracketLeft: precedenceLeft > op.Precedence
                   || precedenceLeft == op.Precedence && op.Associativity == RightToLeft,
        bracketRight: precedenceRight > op.Precedence
                   || precedenceRight == op.Precedence && op.Associativity == LeftToRight);

    /// <summary>
    /// Should an operand expression be bracketed?
    /// </summary>
    /// <param name="operand">An operand of an <paramref name="op"/> operation.</param>
    /// <param name="op">An operator that has <paramref name="operand"/> as one of its operands.</param>
    /// <returns><paramref name="operand"/> should be bracketed when used as an operand of an <paramref name="op"/> operation.</returns>
    public bool ShouldBracketOperand(OperatorInfo op, Expression operand) => GetPrecedence(operand) > op.Precedence;

    public OperatorInfo Get(Expression.BinaryOperation opBin) => Get(opBin.Operator, opBin.Left.Value.Type, opBin.Right.Value.Type);
    public OperatorInfo Get(Expression.UnaryOperation opUn) => Get(opUn.Operator, opUn.Operand.Value.Type);
    protected abstract OperatorInfo Get(BinaryOperator op, EvaluatedType leftType, EvaluatedType rightType);
    protected abstract OperatorInfo Get(UnaryOperator op, EvaluatedType operandType);

    public abstract OperatorInfo Add { get; }
    public abstract OperatorInfo And { get; }
    public abstract OperatorInfo ArraySubscript { get; }
    public abstract OperatorInfo Cast(EvaluatedType target);
    public abstract OperatorInfo ComponentAccess { get; }
    public abstract OperatorInfo Divide { get; }
    public abstract OperatorInfo Equal { get; }
    public abstract OperatorInfo FunctionCall { get; }
    public abstract OperatorInfo GreaterThan { get; }
    public abstract OperatorInfo GreaterThanOrEqual { get; }
    public abstract OperatorInfo LessThan { get; }
    public abstract OperatorInfo LessThanOrEqual { get; }
    public abstract OperatorInfo Mod { get; }
    public abstract OperatorInfo Multiply { get; }
    public abstract OperatorInfo Not { get; }
    public abstract OperatorInfo NotEqual { get; }
    public abstract OperatorInfo Or { get; }
    public abstract OperatorInfo Subtract { get; }
    public abstract OperatorInfo UnaryMinus { get; }
    public abstract OperatorInfo UnaryPlus { get; }

    protected static OperatorInfoAppender Infix(string @operator) => (o, _, p) => {
        Debug.Assert(p.Count == 2);
        return p[1](p[0](o).Append(' ').Append(@operator).Append(' '));
    };
    protected static OperatorInfoAppender Infix(char @operator) => (o, _, p) => {
        Debug.Assert(p.Count == 2);
        return p[1](p[0](o).Append(' ').Append(@operator).Append(' '));
    };

    protected static OperatorInfoAppender Prefix(string @operator) => (o, _, p) => {
        Debug.Assert(p.Count == 1);
        return p[0](o.Append(@operator));
    };
    protected static OperatorInfoAppender Prefix(char @operator) => (o, _, p) => {
        Debug.Assert(p.Count == 1);
        return p[0](o.Append(@operator));
    };

}

