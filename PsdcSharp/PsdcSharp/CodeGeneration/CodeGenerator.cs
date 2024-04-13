namespace Scover.Psdc.CodeGeneration;

internal abstract class CodeGenerator : MessageProvider
{
    public abstract string Generate();
}
