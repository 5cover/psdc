using System.Text;
using Scover.Psdc.Language;
using Scover.Psdc.Messages;
using Scover.Psdc.Parsing;
using Scover.Psdc.StaticAnalysis;
using static Scover.Psdc.Parsing.Node;

namespace Scover.Psdc.CodeGeneration;

public static class CodeGenerator {
    public static string GenerateC(Messenger messenger, SemanticAst ast)
     => new C.CodeGenerator(messenger, ast).Generate();
}

abstract partial class CodeGenerator<TKwTable, TOpTable>(Messenger msger, SemanticAst ast, TKwTable keywordTable, TOpTable operatorTable)
where TKwTable : KeywordTable
where TOpTable : OperatorTable
{
    protected readonly SemanticAst _ast = ast;
    protected readonly Indentation _indent = new();
    protected readonly Messenger _msger = msger;
    protected readonly TKwTable _kwTable = keywordTable;
    protected readonly TOpTable _opTable = operatorTable;

    protected string ValidateIdentifier(Identifier ident) => _kwTable.Validate(ident, _msger);

    public abstract string Generate();

    protected StringBuilder AppendUnaryPrefixOperation<TExpr>(StringBuilder o, ReadOnlyScope scope, OperatorInfo op, TExpr operand, Action<StringBuilder, ReadOnlyScope, TExpr> appender) where TExpr : Expression
    {
        o.Append(op.Code);
        return AppendBracketed(o, _opTable.ShouldBracketUnary(scope, operand, op), o => appender(o, scope, operand));
    }

    protected static StringBuilder AppendBracketed(StringBuilder o, bool bracket, Action<StringBuilder> appender)
    {
        if (bracket) {
            o.Append('(');
        }
        appender(o);
        if (bracket) {
            o.Append(')');
        }
        return o;
    }
}
