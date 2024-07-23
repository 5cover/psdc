using Scover.Psdc.Language;

namespace Scover.Psdc.CodeGeneration;

public interface OperatorInfo<TSelf> where TSelf : OperatorInfo<TSelf>
{
    public static abstract TSelf ArraySubscript { get; }
    public static abstract TSelf ComponentAccess { get; }
    public static abstract TSelf FunctionCall { get; }
    public Associativity Associativity { get; }
    public string Code { get; }
    public int Precedence { get; }

    public static abstract TSelf Get(BinaryOperator op);

    public static abstract TSelf Get(UnaryOperator op);
}
