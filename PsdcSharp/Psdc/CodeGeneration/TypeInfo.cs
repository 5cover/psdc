namespace Scover.Psdc.CodeGeneration;

interface TypeInfo
{
    public string DecorateExpression(string expr);

    public string Generate();

    public string GenerateDeclaration(IEnumerable<string> names);
}
