namespace Scover.Psdc.CodeGeneration;

interface TypeInfo
{
    public string DecorateExpression(string expr);

    public string ToString();

    public string GenerateDeclaration(IEnumerable<string> names);
    public string GenerateDeclaration(string name);
}
