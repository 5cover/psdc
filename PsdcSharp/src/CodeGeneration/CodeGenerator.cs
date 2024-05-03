using Scover.Psdc.Messages;
using Scover.Psdc.StaticAnalysis;

namespace Scover.Psdc.CodeGeneration;

internal abstract partial class CodeGenerator(Messenger messenger, SemanticAst semanticAst)
{
    protected readonly Messenger _messenger = messenger;
    protected readonly SemanticAst _ast = semanticAst;
    protected readonly Indentation _indent = new();
    public abstract string Generate();
}
