namespace Scover.Psdc.CodeGeneration;

internal abstract class CodeGenerator : CompilationStep
{
    public abstract string Generate();
}
