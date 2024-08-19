namespace Scover.Psdc.CodeGeneration;

interface TypeInfo
{
    string DecorateExpression(string expr);
    string ToString();
    string GenerateDeclaration(IEnumerable<string> names);
    string GenerateDeclaration(string name);
}
