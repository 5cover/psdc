using Scover.Psdc.Language;
using Scover.Psdc.Parsing;

using static Scover.Psdc.Parsing.Node;
using static Scover.Psdc.Language.Associativity;

namespace Scover.Psdc.CodeGeneration;

sealed record OperatorInfo(Associativity Associativity, string Code, int Precedence);

abstract class OperatorTable
{
    public abstract OperatorInfo None { get; }
    public abstract OperatorInfo ArraySubscript { get; }
    public abstract OperatorInfo ComponentAccess { get; }
    public abstract OperatorInfo FunctionCall { get; }

    public abstract OperatorInfo Get(BinaryOperator op);
    public abstract OperatorInfo Get(UnaryOperator op);

    public virtual int GetPrecedence(ReadOnlyScope scope, Expression expr)
     => expr switch {
         Expression.FunctionCall or Expression.BuiltinFdf => FunctionCall.Precedence,
         Expression.BinaryOperation opBin => Get(opBin.Operator).Precedence,
         Expression.UnaryOperation opUn => Get(opUn.Operator).Precedence,
         Expression.Lvalue.ArraySubscript => ArraySubscript.Precedence,
         Expression.Lvalue.ComponentAccess => ComponentAccess.Precedence,
         Expression.Lvalue.VariableReference or Expression.Literal or NodeBracketedExpression => int.MinValue,
         _ => throw expr.ToUnmatchedException(),
     };

    public (bool bracketLeft, bool bracketRight) ShouldBracket(ReadOnlyScope scope, Expression.BinaryOperation opBin)
         => ShouldBracketBinary(
            GetPrecedence(scope, opBin.Left),
            GetPrecedence(scope, opBin.Right),
            Get(opBin.Operator));

    public bool ShouldBracket(ReadOnlyScope scope, Expression.UnaryOperation opUn)
     => ShouldBracketUnary(scope, opUn.Operand, Get(opUn.Operator));

    public bool ShouldBracket(ReadOnlyScope scope, Expression.Lvalue.ComponentAccess compAccess)
     => ShouldBracketUnary(scope, compAccess.Structure, ComponentAccess);

    public bool ShouldBracket(ReadOnlyScope scope, Expression.Lvalue.ArraySubscript arrSub)
     => ShouldBracketUnary(scope, arrSub.Array, ArraySubscript);

    private (bool bracketLeft, bool bracketRight) ShouldBracketBinary(int precedenceLeft, int precedenceRight, OperatorInfo op)
     => (bracketLeft: precedenceLeft > op.Precedence
                   || precedenceLeft == op.Precedence && op.Associativity == RightToLeft,
        bracketRight: precedenceRight > op.Precedence
                   || precedenceRight == op.Precedence && op.Associativity == LeftToRight);

    public bool ShouldBracketUnary(ReadOnlyScope scope, Expression operand, OperatorInfo op) => GetPrecedence(scope, operand) > op.Precedence;
}

