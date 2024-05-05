using Scover.Psdc.Language;
using Scover.Psdc.Messages;
using Scover.Psdc.StaticAnalysis;
using static Scover.Psdc.Language.Associativity;
using static Scover.Psdc.Parsing.Node;

namespace Scover.Psdc.CodeGeneration;

abstract partial class CodeGenerator<TOpInfo>(Messenger messenger, SemanticAst semanticAst)
    where TOpInfo : OperatorInfo<TOpInfo>
{
    protected readonly Messenger _messenger = messenger;
    protected readonly SemanticAst _ast = semanticAst;
    protected readonly Indentation _indent = new();
    public abstract string Generate();

    protected static int GetPrecedence(Expression expr)
     => expr switch {
         Expression.FunctionCall or Expression.BuiltinFdf => TOpInfo.FunctionCall.Precedence,
         Expression.BinaryOperation opBin => TOpInfo.Get(opBin.Operator).Precedence,
         Expression.UnaryOperation opUn => TOpInfo.Get(opUn.Operator).Precedence,
         Expression.Lvalue.ArraySubscript => TOpInfo.ArraySubscript.Precedence,
         Expression.Lvalue.ComponentAccess => TOpInfo.ComponentAccess.Precedence,
         Expression.Lvalue.VariableReference or Expression.Literal
             or Expression.Bracketed or Expression.Lvalue.Bracketed => int.MinValue,
         _ => throw expr.ToUnmatchedException(),
     };

    protected static (bool bracketLeft, bool bracketRight) ShouldBracket(Expression.BinaryOperation opBin)
     => ShouldBracket(
        GetPrecedence(opBin.Left),
        GetPrecedence(opBin.Right),
        TOpInfo.Get(opBin.Operator),
        opBin.Operator.GetAssociativity());

    protected static bool ShouldBracket(Expression.UnaryOperation opUn)
     => ShouldBracket(opUn.Operand, TOpInfo.Get(opUn.Operator), opUn.Operator.GetAssociativity());

    protected static bool ShouldBracket(Expression.Lvalue.ComponentAccess compAccess)
     => ShouldBracket(compAccess.Structure, TOpInfo.ComponentAccess, LeftToRight);
    
    protected static bool ShouldBracket(Expression.Lvalue.ArraySubscript arrSub)
     => ShouldBracket(arrSub.Array, TOpInfo.ArraySubscript, LeftToRight);

    static (bool bracketLeft, bool bracketRight) ShouldBracket(int precedenceLeft, int precedenceRight, TOpInfo me, Associativity psdcAssociativity)
     => (bracketLeft: precedenceLeft > me.Precedence
                   || precedenceLeft == me.Precedence
                        && me.Associativity == RightToLeft
                        && psdcAssociativity == LeftToRight,
        bracketRight: precedenceRight > me.Precedence
                   || precedenceRight == me.Precedence
                        && me.Associativity == LeftToRight
                        && psdcAssociativity == RightToLeft);

    static bool ShouldBracket(Expression operand, TOpInfo me, Associativity psdcAssociativity)
    {
        var precedence = GetPrecedence(operand);
        return precedence > me.Precedence
            || precedence == me.Precedence && me.Associativity != psdcAssociativity;
    }
}
