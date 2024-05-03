using Scover.Psdc.Parsing;

namespace Scover.Psdc.CodeGeneration;

internal interface OperatorInfo<TSelf> where TSelf : OperatorInfo<TSelf>
{
    public string Code { get; }
    public Associativity Associativity { get; }
    public int Precedence { get; }

    public static abstract TSelf Get(BinaryOperator op);
    public static abstract TSelf Get(UnaryOperator op);

    public static abstract TSelf ArraySubscript { get; }
    public static abstract TSelf ComponentAccess { get; }
    public static abstract TSelf FunctionCall { get; }
}
