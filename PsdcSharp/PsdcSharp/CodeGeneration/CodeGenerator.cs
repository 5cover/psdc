using Scover.Psdc.SemanticAnalysis;

namespace Scover.Psdc.CodeGeneration;

internal abstract partial class CodeGenerator(SemanticAst semanticAst) : MessageProvider
{
    protected readonly SemanticAst _ast = semanticAst;
    protected readonly Indentation _indent = new();
    public abstract string Generate();
}
