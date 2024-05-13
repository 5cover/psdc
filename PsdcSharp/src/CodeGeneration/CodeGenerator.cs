using Scover.Psdc.CodeGeneration.C;
using Scover.Psdc.Messages;
using Scover.Psdc.Parsing;
using Scover.Psdc.StaticAnalysis;

using static Scover.Psdc.Language.Associativity;
using static Scover.Psdc.Parsing.Node;

namespace Scover.Psdc.CodeGeneration;

public static class CodeGenerator
{
    public static string GenerateC(Messenger messenger, SemanticAst ast)
     => new CodeGeneratorC(messenger, ast).Generate();
}

abstract partial class CodeGenerator<TOpInfo>(Messenger msger, SemanticAst ast)
    where TOpInfo : OperatorInfo<TOpInfo>
{
    protected readonly SemanticAst _ast = ast;
    protected readonly Indentation _indent = new();
    protected readonly Messenger _msger = msger;

    public abstract string Generate();

    protected static int GetPrecedence(Expression expr)
     => expr switch {
         Expression.FunctionCall or Expression.BuiltinFdf => TOpInfo.FunctionCall.Precedence,
         Expression.BinaryOperation opBin => TOpInfo.Get(opBin.Operator).Precedence,
         Expression.UnaryOperation opUn => TOpInfo.Get(opUn.Operator).Precedence,
         Expression.Lvalue.ArraySubscript => TOpInfo.ArraySubscript.Precedence,
         Expression.Lvalue.ComponentAccess => TOpInfo.ComponentAccess.Precedence,
         Expression.Lvalue.VariableReference or Expression.Literal or NodeBracketedExpression => int.MinValue,
         _ => throw expr.ToUnmatchedException(),
     };

    protected static (bool bracketLeft, bool bracketRight) ShouldBracket(Expression.BinaryOperation opBin)
     => ShouldBracket(
        GetPrecedence(opBin.Left),
        GetPrecedence(opBin.Right),
        TOpInfo.Get(opBin.Operator));

    protected static bool ShouldBracket(Expression.UnaryOperation opUn)
     => ShouldBracket(opUn.Operand, TOpInfo.Get(opUn.Operator));

    protected static bool ShouldBracket(Expression.Lvalue.ComponentAccess compAccess)
     => ShouldBracket(compAccess.Structure, TOpInfo.ComponentAccess);

    protected static bool ShouldBracket(Expression.Lvalue.ArraySubscript arrSub)
     => ShouldBracket(arrSub.Array, TOpInfo.ArraySubscript);

    static (bool bracketLeft, bool bracketRight) ShouldBracket(int precedenceLeft, int precedenceRight, TOpInfo me)
     => (bracketLeft: precedenceLeft > me.Precedence
                   || precedenceLeft == me.Precedence && me.Associativity == RightToLeft,
        bracketRight: precedenceRight > me.Precedence
                   || precedenceRight == me.Precedence && me.Associativity == LeftToRight);

    static bool ShouldBracket(Expression operand, TOpInfo me) => GetPrecedence(operand) > me.Precedence;
}
